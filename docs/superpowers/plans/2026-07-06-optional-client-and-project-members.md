# Optional Client + Multiple Project Members Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let admins create a `ClientProject` with no client and attach a primary client later, and let multiple people (one primary client + N collaborators) be associated with one project — either by picking an existing user or inviting by email into the project.

**Architecture:** Keep `ClientProject.ClientId` as the primary-client FK but make it nullable; add a `ProjectMember` join entity for collaborators. "On a project" = primary OR member. Invoices/testimonials stay tied to the primary client. Invitations gain an optional `ProjectId` + `IsPrimaryClient` flag; project linking happens inside `AcceptInvitationAsync`, so the registration page is untouched.

**Tech Stack:** .NET 10, Blazor Server (Interactive Server), EF Core + Npgsql (PostgreSQL), ASP.NET Identity, xUnit + Testcontainers.

## Global Constraints

- Business logic lives in services behind `I…Service` interfaces; components/pages never inject `FellsideDigitalDbContext`.
- EF Core only, parameterised queries; no raw SQL.
- Every admin page/endpoint is `[Authorize(Roles = "SiteAdmin")]`; portal single-project access is enforced server-side (primary-or-member), not just in the UI.
- Never surface `ex.Message` to users: wrap risky ops in try/catch and use `ErrorHandling.LogAndDescribe(Logger, ex, "doing X")`; use `ToastService` for action outcomes.
- Add indexes for new WHERE/join columns.
- Migrations apply automatically on startup; must be additive with no data rewrite.
- **WSL build/EF commands:** build with `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`. EF commands need BOTH `--project` and `--startup-project` pointing at the Web project, e.g. `dotnet.exe ef migrations add X --project src/FellsideDigital.Web --startup-project src/FellsideDigital.Web`. Ignore a flaky `App.razor` `Html` CS0103 generator artifact if it appears.
- Tests: DB-backed tests use `[Collection(PostgresCollection.Name)]` and `fx.CreateContext()` (Docker required). Run with `dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj`.

**Branch first:** this work must go on a feature branch, not `main`.

---

### Task 0: Create the feature branch

**Files:** none (git only)

- [ ] **Step 1: Cut the branch**

```bash
git checkout -b feat/optional-client-and-project-members
git status
```
Expected: `On branch feat/optional-client-and-project-members`, working tree clean.

---

### Task 1: Data model + schema (entities, EF config, migration)

**Files:**
- Create: `src/FellsideDigital.Domain/Enums/ProjectMember.cs`
- Create: `src/FellsideDigital.Web/Data/ProjectMember.cs`
- Modify: `src/FellsideDigital.Web/Data/ClientProject.cs:24` (nullable `ClientId`, add `Members`)
- Modify: `src/FellsideDigital.Web/Data/ClientInvitation.cs` (add `ProjectId`, `IsPrimaryClient`)
- Modify: `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs:11` (DbSet) and `:114-125` (relationships)
- Create: `src/FellsideDigital.Web/Data/Migrations/<generated>_AddProjectMembersAndOptionalClient.cs` (via EF tooling)
- Test: `tests/FellsideDigital.Tests/ProjectMemberSchemaTests.cs`

**Interfaces:**
- Produces:
  - `enum ProjectMemberRole { Collaborator = 0 }` in `FellsideDigital.Domain.Enums`
  - `class ProjectMember { Guid Id; Guid ProjectId; ClientProject? Project; string UserId; ApplicationUser? User; ProjectMemberRole Role; DateTime AddedAt; }`
  - `ClientProject.ClientId` is now `string?`; `ClientProject.Members` is `ICollection<ProjectMember>`
  - `ClientInvitation.ProjectId` is `Guid?`; `ClientInvitation.IsPrimaryClient` is `bool`
  - `FellsideDigitalDbContext.ProjectMembers` `DbSet<ProjectMember>`

- [ ] **Step 1: Add the role enum**

Create `src/FellsideDigital.Domain/Enums/ProjectMember.cs`:

```csharp
namespace FellsideDigital.Domain.Enums;

/// <summary>Role of a non-primary person attached to a project. Reserved for
/// future role distinctions; today every member is a Collaborator.</summary>
public enum ProjectMemberRole
{
    Collaborator = 0
}
```

- [ ] **Step 2: Add the ProjectMember entity**

Create `src/FellsideDigital.Web/Data/ProjectMember.cs`:

```csharp
using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

/// <summary>Join row linking an additional person (collaborator) to a project.
/// The primary client is stored separately on <see cref="ClientProject.ClientId"/>.</summary>
public class ProjectMember
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Collaborator;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Make ClientProject.ClientId nullable and add Members**

In `src/FellsideDigital.Web/Data/ClientProject.cs`, change lines 24-25 from:

```csharp
    public string ClientId { get; set; } = "";
    public ApplicationUser? Client { get; set; }
```

to:

```csharp
    // Primary client (billed). Optional: a project can exist before its client
    // is known; the primary can be set later. Additional people are Members.
    public string? ClientId { get; set; }
    public ApplicationUser? Client { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = [];
```

- [ ] **Step 4: Add invitation → project link fields**

In `src/FellsideDigital.Web/Data/ClientInvitation.cs`, after the `AcceptedUser` property (line 36) add:

```csharp

    /// <summary>When set, accepting this invitation attaches the new user to this
    /// project (as primary if <see cref="IsPrimaryClient"/>, else as a collaborator).
    /// Null for a standalone account-only invitation.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Only meaningful when <see cref="ProjectId"/> is set: attach the new
    /// user as the project's primary client (if it has none yet) rather than a member.</summary>
    public bool IsPrimaryClient { get; set; }
```

- [ ] **Step 5: Register DbSet + relationships**

In `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs`, after line 11 (`ClientProjects` DbSet) add:

```csharp
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
```

Replace the `ClientProject` config block (lines 114-125) with:

```csharp
            builder.Entity<ClientProject>(e =>
            {
                e.HasOne(p => p.Client)
                    .WithMany()
                    .HasForeignKey(p => p.ClientId)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.SetNull);

                e.HasOne(p => p.CreatedByAdmin)
                    .WithMany()
                    .HasForeignKey(p => p.CreatedByAdminId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<ProjectMember>(e =>
            {
                e.HasOne(m => m.Project)
                    .WithMany(p => p.Members)
                    .HasForeignKey(m => m.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(m => m.User)
                    .WithMany()
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One row per (project, user) — prevents duplicate memberships and
                // indexes the join columns used by the access query.
                e.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
            });
```

- [ ] **Step 6: Generate the migration**

Run:

```bash
dotnet.exe ef migrations add AddProjectMembersAndOptionalClient \
  --project src/FellsideDigital.Web --startup-project src/FellsideDigital.Web
```
Expected: a new migration under `src/FellsideDigital.Web/Data/Migrations/`. Open it and confirm it (a) alters `ClientProjects.ClientId` to nullable, (b) creates the `ProjectMembers` table with the unique `(ProjectId, UserId)` index, and (c) adds `ProjectId` + `IsPrimaryClient` to `ClientInvitations`. It must contain **no** `DELETE`/data-rewrite statements.

- [ ] **Step 7: Write the failing schema test**

Create `tests/FellsideDigital.Tests/ProjectMemberSchemaTests.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectMemberSchemaTests(PostgresFixture fx)
{
    [Fact]
    public async Task Project_persists_with_null_client_and_a_member()
    {
        await using var db = fx.CreateContext();

        var user = new ApplicationUser { UserName = $"m{Guid.NewGuid():N}@x.io", Email = "m@x.io" };
        db.Users.Add(user);

        var project = new ClientProject
        {
            Name = "No client yet",
            Description = "Created before the client existed.",
            Status = ProjectStatus.Pending,
            Type = ProjectType.Website,
            ClientId = null,
            CreatedByAdminId = user.Id, // any user id satisfies the restrict FK for this test
        };
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();

        db.ProjectMembers.Add(new ProjectMember { ProjectId = project.Id, UserId = user.Id });
        await db.SaveChangesAsync();

        var reloaded = await db.ClientProjects
            .Include(p => p.Members)
            .SingleAsync(p => p.Id == project.Id);

        Assert.Null(reloaded.ClientId);
        Assert.Single(reloaded.Members);
        Assert.Equal(user.Id, reloaded.Members.First().UserId);
    }
}
```

- [ ] **Step 8: Run the test to verify it passes against the migrated schema**

Run:
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj --filter ProjectMemberSchemaTests
```
Expected: PASS (the fixture applies migrations, so a green run proves the schema change is valid). If Docker is not running, start it first — this test needs Testcontainers.

- [ ] **Step 9: Commit**

```bash
git add src/FellsideDigital.Domain/Enums/ProjectMember.cs \
        src/FellsideDigital.Web/Data/ProjectMember.cs \
        src/FellsideDigital.Web/Data/ClientProject.cs \
        src/FellsideDigital.Web/Data/ClientInvitation.cs \
        src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs \
        src/FellsideDigital.Web/Data/Migrations/ \
        tests/FellsideDigital.Tests/ProjectMemberSchemaTests.cs
git commit -m "feat: optional project client + ProjectMember schema"
```

---

### Task 2: ProjectService — member management + broadened access query

**Files:**
- Modify: `src/FellsideDigital.Web/Services/IProjectService.cs`
- Modify: `src/FellsideDigital.Web/Services/ProjectService.cs` (`GetByIdAsync`, `GetByIdForClientAsync`, `GetForClientAsync`, + 3 new methods)
- Test: `tests/FellsideDigital.Tests/ProjectMemberServiceTests.cs`

**Interfaces:**
- Consumes: `ProjectMember`, `ClientProject`, `FellsideDigitalDbContext.ProjectMembers` (Task 1).
- Produces (added to `IProjectService`):
  - `Task SetPrimaryClientAsync(Guid projectId, string? userId, string actorId)`
  - `Task AddMemberAsync(Guid projectId, string userId, string actorId)`
  - `Task RemoveMemberAsync(Guid projectId, string userId)`
  - `GetForClientAsync(string userId)` returns projects where user is primary **or** a member.

- [ ] **Step 1: Write the failing service tests**

Create `tests/FellsideDigital.Tests/ProjectMemberServiceTests.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class ProjectMemberServiceTests(PostgresFixture fx)
{
    private static ProjectService Sut(FellsideDigitalDbContext db)
        => new(db, storage: null!, new ProjectTimelineService(db));

    private static ApplicationUser NewUser() =>
        new() { UserName = $"u{Guid.NewGuid():N}@x.io", Email = "u@x.io" };

    private static async Task<(string adminId, ClientProject project)> SeedProjectAsync(
        FellsideDigitalDbContext db, string? clientId = null)
    {
        var admin = NewUser();
        db.Users.Add(admin);
        var project = new ClientProject
        {
            Name = "P", Description = "D",
            Status = ProjectStatus.Pending, Type = ProjectType.Website,
            ClientId = clientId, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();
        return (admin.Id, project);
    }

    [Fact]
    public async Task AddMemberAsync_grants_access_and_is_idempotent()
    {
        await using var db = fx.CreateContext();
        var member = NewUser(); db.Users.Add(member); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);

        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId);
        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId); // duplicate

        var rows = await db.ProjectMembers.Where(m => m.ProjectId == project.Id).ToListAsync();
        Assert.Single(rows);

        var forMember = await Sut(db).GetForClientAsync(member.Id);
        Assert.Contains(forMember, p => p.Id == project.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_revokes_access()
    {
        await using var db = fx.CreateContext();
        var member = NewUser(); db.Users.Add(member); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(project.Id, member.Id, adminId);

        await Sut(db).RemoveMemberAsync(project.Id, member.Id);

        var forMember = await Sut(db).GetForClientAsync(member.Id);
        Assert.DoesNotContain(forMember, p => p.Id == project.Id);
    }

    [Fact]
    public async Task GetForClientAsync_returns_projects_where_user_is_primary_or_member_once()
    {
        await using var db = fx.CreateContext();
        var user = NewUser(); db.Users.Add(user); await db.SaveChangesAsync();
        var (adminId, primaryProject) = await SeedProjectAsync(db, clientId: user.Id);
        var (_, memberProject) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(memberProject.Id, user.Id, adminId);

        var result = await Sut(db).GetForClientAsync(user.Id);

        Assert.Contains(result, p => p.Id == primaryProject.Id);
        Assert.Contains(result, p => p.Id == memberProject.Id);
        Assert.Equal(result.Select(p => p.Id).Distinct().Count(), result.Count);
    }

    [Fact]
    public async Task SetPrimaryClientAsync_sets_then_clears_and_demotes_existing_member()
    {
        await using var db = fx.CreateContext();
        var user = NewUser(); db.Users.Add(user); await db.SaveChangesAsync();
        var (adminId, project) = await SeedProjectAsync(db);
        await Sut(db).AddMemberAsync(project.Id, user.Id, adminId);

        await Sut(db).SetPrimaryClientAsync(project.Id, user.Id, adminId);
        var set = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == project.Id);
        Assert.Equal(user.Id, set.ClientId);
        Assert.Empty(set.Members); // promoting a member to primary removes the member row

        await Sut(db).SetPrimaryClientAsync(project.Id, null, adminId);
        var cleared = await db.ClientProjects.AsNoTracking().SingleAsync(p => p.Id == project.Id);
        Assert.Null(cleared.ClientId);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj --filter ProjectMemberServiceTests
```
Expected: FAIL to compile — `IProjectService`/`ProjectService` has no `AddMemberAsync`/`RemoveMemberAsync`/`SetPrimaryClientAsync`.

- [ ] **Step 3: Extend the interface**

In `src/FellsideDigital.Web/Services/IProjectService.cs`, add inside the interface (after `SavePhasesAsync`):

```csharp
    Task SetPrimaryClientAsync(Guid projectId, string? userId, string actorId);
    Task AddMemberAsync(Guid projectId, string userId, string actorId);
    Task RemoveMemberAsync(Guid projectId, string userId);
```

- [ ] **Step 4: Broaden the read queries**

In `src/FellsideDigital.Web/Services/ProjectService.cs`:

In `GetByIdAsync`, add a members include after `.Include(p => p.Client)` (line 41):

```csharp
            .Include(p => p.Members).ThenInclude(m => m.User)
```

In `GetByIdForClientAsync`, add after `.Include(p => p.Client)` (line 51):

```csharp
            .Include(p => p.Members)
```

Replace the `GetForClientAsync` `Where` clause (line 80) from:

```csharp
            .Where(p => p.ClientId == clientId)
```

to:

```csharp
            // "On the project" = the primary client OR any collaborator.
            .Where(p => p.ClientId == clientId || p.Members.Any(m => m.UserId == clientId))
```

- [ ] **Step 5: Implement the three new methods**

Append to `ProjectService` (before the closing brace):

```csharp
    public async Task SetPrimaryClientAsync(Guid projectId, string? userId, string actorId)
    {
        var project = await db.ClientProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return;

        // Promoting an existing collaborator to primary: drop the now-redundant member row.
        if (userId is not null)
        {
            var existing = project.Members.FirstOrDefault(m => m.UserId == userId);
            if (existing is not null) db.ProjectMembers.Remove(existing);
        }

        project.ClientId = userId;
        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task AddMemberAsync(Guid projectId, string userId, string actorId)
    {
        var project = await db.ClientProjects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        if (project is null) return;

        // Idempotent: no-op if already the primary client or already a member.
        if (project.ClientId == userId) return;
        if (project.Members.Any(m => m.UserId == userId)) return;

        db.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = ProjectMemberRole.Collaborator,
            AddedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid projectId, string userId)
    {
        var member = await db.ProjectMembers
            .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == userId);
        if (member is null) return;

        db.ProjectMembers.Remove(member);
        await db.SaveChangesAsync();
    }
```

Add `using FellsideDigital.Domain.Enums;` at the top if not already present (it is — line 1).

- [ ] **Step 6: Run the tests to verify they pass**

Run:
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj --filter ProjectMemberServiceTests
```
Expected: PASS (4 tests).

- [ ] **Step 7: Commit**

```bash
git add src/FellsideDigital.Web/Services/IProjectService.cs \
        src/FellsideDigital.Web/Services/ProjectService.cs \
        tests/FellsideDigital.Tests/ProjectMemberServiceTests.cs
git commit -m "feat: project member add/remove + primary-or-member access query"
```

---

### Task 3: Invitation → project linking on acceptance

**Files:**
- Modify: `src/FellsideDigital.Web/Services/InvitationService.cs` (`AcceptInvitationAsync`)
- Test: `tests/FellsideDigital.Tests/InvitationProjectLinkTests.cs`

**Interfaces:**
- Consumes: `ClientInvitation.ProjectId`, `ClientInvitation.IsPrimaryClient` (Task 1); `ProjectMember` (Task 1).
- Produces: `AcceptInvitationAsync(Guid invitationId, string newUserId)` now links the user to `ProjectId` when set — as primary if `IsPrimaryClient` and the project has no primary yet, else as a member.

- [ ] **Step 1: Write the failing tests**

Create `tests/FellsideDigital.Tests/InvitationProjectLinkTests.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class InvitationProjectLinkTests(PostgresFixture fx)
{
    // NavigationManager/IEmailService are only used by CreateInvitationAsync, not by
    // AcceptInvitationAsync, so the accept path can be tested with those deps null.
    private static InvitationService Sut(FellsideDigitalDbContext db)
        => new(db, emailService: null!, navigationManager: null!,
               logger: new Microsoft.Extensions.Logging.Abstractions.NullLogger<InvitationService>());

    private static async Task<(string userId, Guid projectId, Guid invitationId)> SeedAsync(
        FellsideDigitalDbContext db, bool isPrimary, string? existingPrimary = null)
    {
        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        var user = new ApplicationUser { UserName = $"u{Guid.NewGuid():N}@x.io", Email = "u@x.io" };
        db.Users.AddRange(admin, user);
        var project = new ClientProject
        {
            Name = "P", Description = "D", Status = ProjectStatus.Pending, Type = ProjectType.Website,
            ClientId = existingPrimary, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);
        var invite = new ClientInvitation
        {
            Id = Guid.NewGuid(), Token = Guid.NewGuid().ToString("N"), Email = "u@x.io",
            FirstName = "U", LastName = "Ser", Status = InvitationStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddDays(7), CreatedByUserId = admin.Id,
            ProjectId = project.Id, IsPrimaryClient = isPrimary,
        };
        db.ClientInvitations.Add(invite);
        await db.SaveChangesAsync();
        return (user.Id, project.Id, invite.Id);
    }

    [Fact]
    public async Task Accept_primary_invite_sets_client_when_project_has_none()
    {
        await using var db = fx.CreateContext();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: true);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Equal(userId, project.ClientId);
        Assert.Empty(project.Members);
    }

    [Fact]
    public async Task Accept_collaborator_invite_adds_member()
    {
        await using var db = fx.CreateContext();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: false);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Null(project.ClientId);
        Assert.Contains(project.Members, m => m.UserId == userId);
    }

    [Fact]
    public async Task Accept_primary_invite_falls_back_to_member_when_primary_taken()
    {
        await using var db = fx.CreateContext();
        var occupant = new ApplicationUser { UserName = $"o{Guid.NewGuid():N}@x.io", Email = "o@x.io" };
        db.Users.Add(occupant); await db.SaveChangesAsync();
        var (userId, projectId, inviteId) = await SeedAsync(db, isPrimary: true, existingPrimary: occupant.Id);

        await Sut(db).AcceptInvitationAsync(inviteId, userId);

        var project = await db.ClientProjects.Include(p => p.Members).SingleAsync(p => p.Id == projectId);
        Assert.Equal(occupant.Id, project.ClientId);
        Assert.Contains(project.Members, m => m.UserId == userId);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run:
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj --filter InvitationProjectLinkTests
```
Expected: FAIL — the accepted invite never sets `ClientId` or adds a `ProjectMember` (all three assertions fail).

- [ ] **Step 3: Implement project linking in AcceptInvitationAsync**

In `src/FellsideDigital.Web/Services/InvitationService.cs`, replace the body of `AcceptInvitationAsync` (lines 62-72) with:

```csharp
    public async Task AcceptInvitationAsync(Guid invitationId, string newUserId)
    {
        var invitation = await db.ClientInvitations.FindAsync(invitationId);
        if (invitation is null) return;

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedAt = DateTime.UtcNow;
        invitation.AcceptedUserId = newUserId;

        // If the invitation was scoped to a project, attach the new user to it.
        if (invitation.ProjectId is { } projectId)
        {
            var project = await db.ClientProjects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project is not null)
            {
                var alreadyPrimary = project.ClientId == newUserId;
                var alreadyMember = project.Members.Any(m => m.UserId == newUserId);

                // Primary invite only claims the primary slot when it is still empty;
                // otherwise (and for collaborator invites) the user joins as a member.
                if (invitation.IsPrimaryClient && project.ClientId is null)
                {
                    project.ClientId = newUserId;
                    project.UpdatedAt = DateTime.UtcNow;
                }
                else if (!alreadyPrimary && !alreadyMember)
                {
                    db.ProjectMembers.Add(new ProjectMember
                    {
                        Id = Guid.NewGuid(),
                        ProjectId = projectId,
                        UserId = newUserId,
                        Role = ProjectMemberRole.Collaborator,
                        AddedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
```

Confirm `using FellsideDigital.Domain.Enums;` is present at the top of the file (it is — line 1).

- [ ] **Step 4: Run the tests to verify they pass**

Run:
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj --filter InvitationProjectLinkTests
```
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Services/InvitationService.cs \
        tests/FellsideDigital.Tests/InvitationProjectLinkTests.cs
git commit -m "feat: link invitation to project on acceptance (primary or member)"
```

---

### Task 4: Admin — create a project without a client

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor:42-51` (client field optional)
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor:24-27` (copy)
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs:95,143` (nullable client)

**Interfaces:**
- Consumes: `ProjectService.CreateAsync` (unchanged), nullable `ClientProject.ClientId` (Task 1).

- [ ] **Step 1: Make the client input optional in the form**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor`, replace the `FormField Label="Client"` block (lines 42-51) with:

```razor
            <FormField Label="Client" Hint="(optional — you can set this later)">
                <InputSelect @bind-Value="Input.ClientId" class="@FieldStyles.Input">
                    <option value="">— No client yet —</option>
                    @foreach (var client in _clients)
                    {
                        <option value="@client.Id">@client.CompanyName — @client.Email</option>
                    }
                </InputSelect>
            </FormField>
```

- [ ] **Step 2: Soften the page description copy**

In the same file, replace the intro paragraph (lines 25-27) with:

```razor
    <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">
        Set up a project now — you can assign a client and invite collaborators at any time from the project page.
    </p>
```

- [ ] **Step 3: Drop `[Required]` and null-normalise the client id**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs`:

Change line 143 from:
```csharp
        [Required] public string ClientId { get; set; } = "";
```
to:
```csharp
        public string ClientId { get; set; } = "";
```

Change line 95 from:
```csharp
                ClientId = Input.ClientId,
```
to:
```csharp
                ClientId = string.IsNullOrWhiteSpace(Input.ClientId) ? null : Input.ClientId,
```

- [ ] **Step 4: Build and verify**

Run:
```bash
dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj
```
Expected: Build succeeded (ignore any flaky `App.razor` `Html` CS0103 artifact).

Manual check (run the app per CLAUDE.md, or rebuild in VS Docker on :8080): go to `/Admin/Projects/Create`, leave the client as "— No client yet —", fill name + description, submit. Expect redirect to the new project's detail page with no client assigned and no validation error on the client field.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs
git commit -m "feat: allow creating a project without a client"
```

---

### Task 5: Admin — People card on project detail (set primary, add/remove collaborators, invite)

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PeopleCard.razor`
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor` (render `<PeopleCard>` + null-safe client panel)
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs:112-113` (null-guard testimonial lookup)
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PortalPreview.razor.cs:29` (guard no-primary)

**Interfaces:**
- Consumes: `IProjectService.SetPrimaryClientAsync/AddMemberAsync/RemoveMemberAsync` (Task 2); `IInvitationService.CreateInvitationAsync` (existing); `ClientProject.Members`, `ProjectMember` (Task 1).
- Produces: a self-contained `PeopleCard` component taking `[Parameter] ClientProject Project`, `[Parameter] EventCallback OnChanged`.

**Design note:** `PeopleCard` owns all people mutations so `Detail.razor.cs` stays an orchestrator. It injects the services it needs, calls them, shows toasts via `ToastService`, and raises `OnChanged` so the parent reloads the project. "Existing user" pickers list non-`SiteAdmin` users excluding anyone already on the project.

- [ ] **Step 1: Create the PeopleCard component markup**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PeopleCard.razor`:

```razor
@using FellsideDigital.Web.Data
@using FellsideDigital.UI.Components.Forms
@using FellsideDigital.UI.Components.Feedback

<div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm">
    <div class="px-6 sm:px-8 py-5 border-b border-gray-100 dark:border-white/5">
        <h2 class="text-sm font-semibold text-gray-900 dark:text-white">People</h2>
        <p class="text-xs text-gray-500 dark:text-neutral-400 mt-0.5">The primary client is billed; collaborators can view the project in their portal.</p>
    </div>

    <div class="px-6 sm:px-8 py-5 space-y-6">
        <!-- Primary client -->
        <div>
            <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-2">Primary client</p>
            @if (Project.Client is not null)
            {
                <div class="flex items-center justify-between gap-3">
                    <div class="min-w-0">
                        <p class="text-sm font-medium text-gray-900 dark:text-white truncate">@DisplayName(Project.Client)</p>
                        <p class="text-xs text-gray-500 dark:text-neutral-400 truncate">@Project.Client.Email</p>
                    </div>
                    <button type="button" @onclick="ClearPrimaryAsync"
                            class="text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-red-500 transition-colors">
                        Clear
                    </button>
                </div>
            }
            else
            {
                <p class="text-sm text-gray-500 dark:text-neutral-400 mb-3">No primary client yet.</p>
                <div class="flex flex-wrap items-end gap-2">
                    <select @bind="_selectedPrimaryId" class="@FieldStyles.Input max-w-xs">
                        <option value="">Select an existing user…</option>
                        @foreach (var u in AssignableUsers())
                        {
                            <option value="@u.Id">@DisplayName(u) — @u.Email</option>
                        }
                    </select>
                    <button type="button" @onclick="SetPrimaryAsync" disabled="@string.IsNullOrEmpty(_selectedPrimaryId)"
                            class="rounded-xl bg-accent text-white px-3.5 py-2 text-xs font-semibold disabled:opacity-40 transition-colors">
                        Set primary
                    </button>
                    <button type="button" @onclick="() => OpenInvite(true)"
                            class="rounded-xl bg-accent/10 hover:bg-accent/20 text-accent px-3.5 py-2 text-xs font-semibold transition-colors">
                        Invite by email
                    </button>
                </div>
            }
        </div>

        <!-- Collaborators -->
        <div class="border-t border-gray-100 dark:border-white/5 pt-5">
            <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-2">Collaborators</p>
            @if (Project.Members.Count == 0)
            {
                <p class="text-sm text-gray-500 dark:text-neutral-400 mb-3">No collaborators yet.</p>
            }
            else
            {
                <ul class="space-y-2 mb-3">
                    @foreach (var m in Project.Members)
                    {
                        <li class="flex items-center justify-between gap-3">
                            <div class="min-w-0">
                                <p class="text-sm font-medium text-gray-900 dark:text-white truncate">@DisplayName(m.User)</p>
                                <p class="text-xs text-gray-500 dark:text-neutral-400 truncate">@(m.User?.Email)</p>
                            </div>
                            <button type="button" @onclick="() => RemoveMemberAsync(m.UserId)"
                                    class="text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-red-500 transition-colors">
                                Remove
                            </button>
                        </li>
                    }
                </ul>
            }
            <div class="flex flex-wrap items-end gap-2">
                <select @bind="_selectedMemberId" class="@FieldStyles.Input max-w-xs">
                    <option value="">Add an existing user…</option>
                    @foreach (var u in AssignableUsers())
                    {
                        <option value="@u.Id">@DisplayName(u) — @u.Email</option>
                    }
                </select>
                <button type="button" @onclick="AddMemberAsync" disabled="@string.IsNullOrEmpty(_selectedMemberId)"
                        class="rounded-xl bg-accent text-white px-3.5 py-2 text-xs font-semibold disabled:opacity-40 transition-colors">
                    Add
                </button>
                <button type="button" @onclick="() => OpenInvite(false)"
                        class="rounded-xl bg-accent/10 hover:bg-accent/20 text-accent px-3.5 py-2 text-xs font-semibold transition-colors">
                    Invite by email
                </button>
            </div>
        </div>
    </div>
</div>

<Modal IsOpen="_showInvite" OnBackdropClick="() => _showInvite = false">
    <div class="space-y-4">
        <h3 class="text-sm font-semibold text-gray-900 dark:text-white">
            @(_inviteAsPrimary ? "Invite primary client" : "Invite collaborator")
        </h3>
        <AlertBanner Message="@_inviteError" Variant="error" />
        <div class="grid grid-cols-2 gap-3">
                <div>
                    <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">First name</label>
                    <input type="text" @bind="_inviteFirstName" class="@FieldStyles.Input" />
                </div>
                <div>
                    <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Last name</label>
                    <input type="text" @bind="_inviteLastName" class="@FieldStyles.Input" />
                </div>
            </div>
            <div>
                <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Email</label>
                <input type="email" @bind="_inviteEmail" class="@FieldStyles.Input" />
            </div>
            <div class="flex items-center justify-end gap-3 pt-2">
                <button type="button" @onclick="() => _showInvite = false"
                        class="text-sm font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">Cancel</button>
                <button type="button" @onclick="SendInviteAsync" disabled="@_sendingInvite"
                        class="rounded-xl bg-accent text-white px-4 py-2 text-sm font-semibold disabled:opacity-40 transition-colors">
                    @(_sendingInvite ? "Sending…" : "Send invitation")
                </button>
            </div>
        </div>
    </Modal>
```

- [ ] **Step 2: Create the PeopleCard code-behind**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PeopleCard.razor.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PeopleCard : ComponentBase
{
    [Parameter, EditorRequired] public ClientProject Project { get; set; } = default!;
    [Parameter] public EventCallback OnChanged { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvitationService InvitationService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<PeopleCard> Logger { get; set; } = default!;

    private List<ApplicationUser> _clients = [];
    private string _selectedPrimaryId = "";
    private string _selectedMemberId = "";

    private bool _showInvite;
    private bool _inviteAsPrimary;
    private bool _sendingInvite;
    private string? _inviteError;
    private string _inviteFirstName = "";
    private string _inviteLastName = "";
    private string _inviteEmail = "";

    protected override async Task OnInitializedAsync()
    {
        var all = UserManager.Users.ToList();
        var adminIds = (await UserManager.GetUsersInRoleAsync("SiteAdmin")).Select(u => u.Id).ToHashSet();
        _clients = all.Where(u => !adminIds.Contains(u.Id)).ToList();
    }

    private IEnumerable<ApplicationUser> AssignableUsers()
    {
        var onProject = new HashSet<string>(Project.Members.Select(m => m.UserId));
        if (Project.ClientId is not null) onProject.Add(Project.ClientId);
        return _clients.Where(u => !onProject.Contains(u.Id));
    }

    private static string DisplayName(ApplicationUser? u)
    {
        if (u is null) return "Unknown";
        var full = $"{u.FirstName} {u.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(full)) return full;
        if (!string.IsNullOrWhiteSpace(u.CompanyName)) return u.CompanyName!;
        return u.Email ?? "Unknown";
    }

    private async Task<string?> AdminIdAsync()
    {
        var state = await AuthState.GetAuthenticationStateAsync();
        return (await UserManager.GetUserAsync(state.User))?.Id;
    }

    private async Task SetPrimaryAsync()
    {
        if (string.IsNullOrEmpty(_selectedPrimaryId)) return;
        await MutateAsync(async adminId =>
        {
            await ProjectService.SetPrimaryClientAsync(Project.Id, _selectedPrimaryId, adminId);
            _selectedPrimaryId = "";
        }, "setting the primary client", "Primary client set.");
    }

    private async Task ClearPrimaryAsync() =>
        await MutateAsync(async adminId =>
            await ProjectService.SetPrimaryClientAsync(Project.Id, null, adminId),
            "clearing the primary client", "Primary client cleared.");

    private async Task AddMemberAsync()
    {
        if (string.IsNullOrEmpty(_selectedMemberId)) return;
        await MutateAsync(async adminId =>
        {
            await ProjectService.AddMemberAsync(Project.Id, _selectedMemberId, adminId);
            _selectedMemberId = "";
        }, "adding the collaborator", "Collaborator added.");
    }

    private async Task RemoveMemberAsync(string userId) =>
        await MutateAsync(async _ =>
            await ProjectService.RemoveMemberAsync(Project.Id, userId),
            "removing the collaborator", "Collaborator removed.");

    private async Task MutateAsync(Func<string, Task> action, string doing, string success)
    {
        try
        {
            var adminId = await AdminIdAsync();
            if (adminId is null) { Toasts.Error("Could not identify admin user."); return; }
            await action(adminId);
            Toasts.Success(success);
            await OnChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, doing));
        }
    }

    private void OpenInvite(bool asPrimary)
    {
        _inviteAsPrimary = asPrimary;
        _inviteError = null;
        _inviteFirstName = _inviteLastName = _inviteEmail = "";
        _showInvite = true;
    }

    private async Task SendInviteAsync()
    {
        _inviteError = null;
        if (string.IsNullOrWhiteSpace(_inviteEmail) || string.IsNullOrWhiteSpace(_inviteFirstName))
        {
            _inviteError = "First name and email are required.";
            return;
        }

        _sendingInvite = true;
        try
        {
            var adminId = await AdminIdAsync();
            if (adminId is null) { _inviteError = "Could not identify admin user."; return; }

            var invitation = new ClientInvitation
            {
                Email = _inviteEmail.Trim(),
                FirstName = _inviteFirstName.Trim(),
                LastName = _inviteLastName.Trim(),
                ProjectId = Project.Id,
                IsPrimaryClient = _inviteAsPrimary,
            };

            var (_, emailError) = await InvitationService.CreateInvitationAsync(invitation, adminId);
            _showInvite = false;
            if (emailError is null)
                Toasts.Success($"Invitation sent to {invitation.Email}.");
            else
                Toasts.Error("Invitation created, but the email failed to send.");

            await OnChanged.InvokeAsync();
        }
        catch (Exception ex)
        {
            _inviteError = ErrorHandling.LogAndDescribe(Logger, ex, "sending the invitation");
        }
        finally
        {
            _sendingInvite = false;
        }
    }
}
```

- [ ] **Step 3: Render the People card on the detail page**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor`, locate the client overview panel (around lines 120-185). Immediately after the closing `</div>` of that client card, insert:

```razor
        <PeopleCard Project="_project" OnChanged="LoadAsync" class="mt-6" />
```

If the surrounding markup already wraps sidebar cards in a vertical stack (e.g. `space-y-6`), drop the `class="mt-6"`. Ensure `_project` is non-null at this point (it renders inside the existing `@if (_project is not null)` guard).

- [ ] **Step 4: Null-guard the testimonial lookup and no-primary UI**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs`, replace lines 112-113:

```csharp
        _clientHasTestimonial = _project is not null
            && await Testimonials.GetForUserAsync(_project.ClientId) is not null;
```

with:

```csharp
        _clientHasTestimonial = _project?.ClientId is { } cid
            && await Testimonials.GetForUserAsync(cid) is not null;
```

In `Detail.razor`, any block that unconditionally used `_project.Client!.X` or `_project.ClientId` for links (e.g. the invoices link at line 371 `/Admin/Clients/{_project.ClientId}/Invoices`) must be wrapped so it only renders when `_project.Client is not null`. Wrap the testimonial-request and invoices-link controls in `@if (_project.Client is not null) { … }`.

- [ ] **Step 5: Guard PortalPreview against a missing primary**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PortalPreview.razor.cs`, at the top of the method that reads the project (around line 26-29, before `PreviewState.Enter(project.ClientId, …)`), add:

```csharp
        if (project.ClientId is null)
        {
            NavigationManager.NavigateTo($"/Admin/Projects/{project.Id}");
            return;
        }
```

Confirm `NavigationManager` is injected in that component; if not, inject it (`[Inject] private NavigationManager NavigationManager { get; set; } = default!;`). This prevents "preview as client" when there is no client to emulate. Additionally, in `Detail.razor`, hide/disable the "Preview as client" button when `_project.Client is null` with a short note ("Set a primary client to preview their portal.").

- [ ] **Step 6: Build and verify**

Run:
```bash
dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj
```
Expected: Build succeeded.

Manual check (VS Docker :8080 or `dotnet run`): open a project with no client → People card shows "No primary client yet"; set primary from the dropdown → toast + client appears; add a collaborator → appears in list; remove → disappears; "Invite by email" (collaborator) → sends invite (or shows the on-screen link in dev). Confirm the "Preview as client" control is hidden while there is no primary.

- [ ] **Step 7: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/PeopleCard.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/PeopleCard.razor.cs \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor.cs \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/PortalPreview.razor.cs
git commit -m "feat: People card — set primary, manage collaborators, invite into project"
```

---

### Task 6: Portal — let collaborators view the project

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs:32-37` (primary-or-member access check)

**Interfaces:**
- Consumes: `GetByIdForClientAsync` now includes `Members` (Task 2).

- [ ] **Step 1: Broaden the single-project access check**

In `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs`, replace the guard (lines 32-37) that currently reads:

```csharp
        var clientId = PreviewState.ResolveClientId(user.Id, authState.User.IsInRole("SiteAdmin"));
        _project = await ProjectService.GetByIdForClientAsync(Id);

        if (_project is null || _project.ClientId != clientId)
```

with:

```csharp
        var clientId = PreviewState.ResolveClientId(user.Id, authState.User.IsInRole("SiteAdmin"));
        _project = await ProjectService.GetByIdForClientAsync(Id);

        // Access = the primary client OR any collaborator on the project.
        var hasAccess = _project is not null
            && (_project.ClientId == clientId || _project.Members.Any(m => m.UserId == clientId));
        if (!hasAccess)
```

Keep whatever the original `if` body did (redirect / not-found). Verify the exact surrounding lines when editing — line numbers are indicative.

- [ ] **Step 2: Build and verify**

Run:
```bash
dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj
```
Expected: Build succeeded.

Manual check: as a collaborator (a user added to a project but not its primary), the project appears in `/Portal` and its detail page opens; a user who is neither primary nor member is denied the detail page.

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs
git commit -m "feat: portal project access for collaborators (primary or member)"
```

---

### Task 7: Full suite + finish

**Files:** none (verification)

- [ ] **Step 1: Run the whole test suite**

Run (Docker must be up):
```bash
dotnet.exe test tests/FellsideDigital.Tests/FellsideDigital.Tests.csproj
```
Expected: all tests PASS, including the pre-existing `ProjectServiceTests` (which construct `new ProjectService(db, null!, null!)` — confirm the read-query changes didn't break the count test).

- [ ] **Step 2: Full solution build**

Run:
```bash
dotnet.exe build
```
Expected: Build succeeded across all projects.

- [ ] **Step 3: Finish the branch**

Use the `superpowers:finishing-a-development-branch` skill to decide merge vs PR and complete the work.
