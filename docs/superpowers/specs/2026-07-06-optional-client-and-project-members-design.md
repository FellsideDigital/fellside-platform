# Optional client + multiple people per project

**Date:** 2026-07-06
**Status:** Approved — ready for implementation plan

## Problem

Today a `ClientProject` requires exactly one client via a non-nullable `ClientId`
foreign key, set at creation time from a dropdown of existing users. Two gaps:

1. You cannot create a project before its client exists / is decided, then attach
   the client later.
2. Only one person can be associated with a project. Real projects often involve
   several stakeholders who should all be able to see progress in the portal.

## Goals

- Create a project with **no client**, then set the primary client afterwards.
- Attach **multiple people** to one project — one primary client plus any number
  of collaborators.
- Attach people either by **picking an existing user** or by **inviting by email**,
  where the invitation ties the person to the project on registration.

## Non-goals

- No change to how invoices or testimonials are owned — they stay tied to the
  **primary client** only.
- No per-member permission model. Collaborators get the same portal *project* view
  as the primary (progress, timeline, documents). Invoices remain personal-to-user
  (they already are), so a collaborator simply sees their own invoices.
- No rename of the existing `ClientId` column — kept as-is to minimise blast radius.

## Design

### Member model: one primary + collaborators

A project has an optional **primary client** (the existing `ClientId`, now nullable)
and zero or more **collaborators** via a new join entity. "On the project" =
primary **or** a member.

### Data model

Keep `ClientProject.ClientId` as the primary-client FK, but make it **nullable**.
Add a join entity:

```csharp
// ClientProject.cs — change
public string? ClientId { get; set; }          // was: string = ""  (now nullable)
public ApplicationUser? Client { get; set; }   // unchanged
public ICollection<ProjectMember> Members { get; set; } = [];  // new

// ProjectMember.cs — new
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

// Domain/Enums — new
public enum ProjectMemberRole { Collaborator = 0 }  // reserved for future roles
```

**EF configuration** (`FellsideDigitalDbContext`):

- `ClientProject.ClientId` → the relationship is currently
  `.HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict)`. Change to
  `.IsRequired(false).OnDelete(DeleteBehavior.SetNull)` so an unset primary is valid
  and deleting a user unsets the primary rather than being blocked (mirrors the
  existing nullable `ApplicationUser.Invitation` relationship).
- `ProjectMember`: FK to `ClientProject` (cascade delete with the project), FK to
  `ApplicationUser` (cascade delete when the user is removed). Unique composite
  index on `(ProjectId, UserId)` to prevent duplicates.

**Migration** is additive: drop the NOT NULL on `ClientProjects.ClientId`, create
the `ProjectMembers` table + indexes. No data rewrite.

### Access rule (single source of truth)

A user sees a project when `p.ClientId == userId || p.Members.Any(m => m.UserId == userId)`.
Apply this in `ProjectService.GetForClientAsync` and anywhere the portal authorises a
single-project view (e.g. `ProjectDetail` currently checks `_project.ClientId != clientId`
— becomes "not primary and not a member → deny").

### Service surface (`IProjectService` / `ProjectService`)

New methods (all record a client-visible timeline event where meaningful, via the
existing `IProjectTimelineService`):

- `Task SetPrimaryClientAsync(Guid projectId, string? userId, string actorId)`
  — set or clear the primary client.
- `Task AddMemberAsync(Guid projectId, string userId, string actorId)`
  — idempotent (no-op if already primary or already a member).
- `Task RemoveMemberAsync(Guid projectId, string userId)`.

Includes to extend so members load with the project:

- `GetByIdAsync`: add `.Include(p => p.Members).ThenInclude(m => m.User)`.
- `GetForClientAsync`: broaden the `Where` to the access rule above; the method must
  still only return each project once.

### Admin: create project without a client

`Admin/Projects/Create`:

- Client `<InputSelect>` gains a leading `— No client yet —` option (value `""`).
- Remove `[Required]` from `InputModel.ClientId`.
- When building the entity: `ClientId = string.IsNullOrWhiteSpace(Input.ClientId) ? null : Input.ClientId`.
- Copy tweak: the page currently says "Assign a project to a client so they can track
  progress" — soften to note the client is optional and can be added later.

Null-guard any `_project.Client!.X` usages that assumed a non-null client (most already
use `_project.Client?.X`). The `PortalPreview` "preview as client" path requires a
primary client — when there is none, disable/hide that action with a short note.

### Admin: attach people after the fact (project detail)

Add a **People** card to `Admin/Projects/Detail`:

- **Primary client** row:
  - Unset → "Set primary client" control offering **pick existing user** or
    **invite by email**.
  - Set → shows the user (company + email) with **Change** / **Clear** actions.
- **Collaborators** list: each row shows the user with a **Remove** action; an
  **Add collaborator** control offers **pick existing user** or **invite by email**.

All data mutations go through the new `IProjectService` methods (or
`IInvitationService` for the invite path). Use `ToastService` for outcomes and
`ErrorHandling.LogAndDescribe` for failures, per project conventions.

"Existing users" for the pickers = non-`SiteAdmin` users (same filter
`Admin/Projects/Create` already uses), excluding anyone already on the project.

### Invitations tied to a project

Extend the existing `ClientInvitation` (reuse, do not fork):

```csharp
public Guid? ProjectId { get; set; }        // new — null = standalone account invite
public bool  IsPrimaryClient { get; set; }  // new — meaningful only when ProjectId set
```

- `Admin/Projects/Detail` invite paths create a `ClientInvitation` with `ProjectId`
  set and `IsPrimaryClient` = true for the primary-client invite, false for a
  collaborator invite. `CreateInvitationAsync` is otherwise unchanged (token, email).
- `InvitationService.AcceptInvitationAsync(invitationId, newUserId)` gains project
  linking: after marking the invite accepted, if `ProjectId` is set —
  - if `IsPrimaryClient` **and** the project's `ClientId` is still null → set
    `ClientId = newUserId`;
  - otherwise → add a `ProjectMember` (idempotent).
- Standalone invitations (`ProjectId == null`) behave exactly as today.
- Migration: add the two nullable columns to `ClientInvitations`.

### Registration flow

`Register.razor` already calls `AcceptInvitationAsync` after creating the user; the
new project-linking lives inside that method, so `Register.razor` needs **no change**.

## Affected files (indicative)

- `Domain/Enums/` — new `ProjectMemberRole`.
- `Web/Data/ClientProject.cs` — nullable `ClientId`, `Members` collection.
- `Web/Data/ProjectMember.cs` — new.
- `Web/Data/ClientInvitation.cs` — `ProjectId`, `IsPrimaryClient`.
- `Web/Data/FellsideDigitalDbContext.cs` — DbSet + relationships + indexes.
- `Web/Data/Migrations/` — one additive migration.
- `Web/Services/IProjectService.cs` / `ProjectService.cs` — new methods + includes +
  broadened access query.
- `Web/Services/IInvitationService.cs` / `InvitationService.cs` — project linking on accept.
- `Web/Components/Pages/Admin/Projects/Create.razor(.cs)` — optional client.
- `Web/Components/Pages/Admin/Projects/Detail.razor(.cs)` — People card.
- `Web/Components/Pages/Admin/Projects/PortalPreview.razor.cs` — guard no-primary case.
- `Web/Components/Pages/Portal/ProjectDetail.razor.cs` — access rule.
- `Web/Components/Layout/PortalLayout.razor.cs` and portal pages using
  `GetForClientAsync` — behaviour follows the broadened query; verify no assumptions break.

## Testing (Testcontainers / Postgres)

- Create a project with `ClientId == null`; it persists and loads.
- `AddMemberAsync` then `GetForClientAsync(memberId)` returns the project; duplicate
  add is a no-op; `RemoveMemberAsync` removes access.
- `GetForClientAsync` returns projects where the user is primary **or** a member,
  each project once, no duplicates.
- `SetPrimaryClientAsync` sets and clears `ClientId` and records a timeline event.
- `AcceptInvitationAsync` with `ProjectId` + `IsPrimaryClient=true` sets `ClientId`
  when empty; with `IsPrimaryClient=false` (or when a primary already exists) adds a
  `ProjectMember` instead.

## Security checklist

- New admin People actions and invite endpoints require `[Authorize(Roles = "SiteAdmin")]`
  (match existing project admin pages).
- Portal single-project access enforces the primary-or-member rule server-side, not
  just in the UI.
- No secrets, EF Core parameterised queries only, no exception detail to users
  (`ErrorHandling.LogAndDescribe` + toasts).
- New `WHERE`/join columns (`ProjectMembers.UserId`, `ProjectId`) are indexed.
