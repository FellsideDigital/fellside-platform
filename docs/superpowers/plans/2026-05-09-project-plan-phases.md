# Project Plan Phases Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add structured project phase planning (up to 5 ordered phases with status tracking) and TargetLaunchDate datepicker to the admin project create/edit/detail pages.

**Architecture:** New `ProjectPlanPhase` entity stored in its own table with cascade-delete from `ClientProject`. Phases are replaced atomically on save via `SavePhasesAsync`. The phase editor is a component-level list (`List<PhaseEditorModel>`) managed outside the EditForm model — button handlers mutate it directly, and it is persisted after the main project save.

**Tech Stack:** Blazor Server, EF Core 8, PostgreSQL (Npgsql), Tailwind CSS dark theme, existing `InputClass` constant, `SpinnerButton`/`AlertBanner`/`CardSection` shared components, `EnumExtensions.ToOptions<T>()`, `BadgeHelpers`.

---

## File Map

| File | Action | What changes |
|---|---|---|
| `src/FellsideDigital.Domain/Enums/Project.cs` | Modify | Add `PhaseStatus` enum |
| `src/FellsideDigital.Web/Data/ProjectPlanPhase.cs` | Create | New entity |
| `src/FellsideDigital.Web/Data/ClientProject.cs` | Modify | Add `PlanPhases` nav prop |
| `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs` | Modify | Add `DbSet`, EF cascade config |
| `src/FellsideDigital.UI/Helpers/BadgeHelpers.cs` | Modify | Add `PhaseStatusBadge`, `PhaseStatusDotColor`, `PhaseStatusLabel` |
| `src/FellsideDigital.Web/Services/IProjectService.cs` | Modify | Add `SavePhasesAsync` |
| `src/FellsideDigital.Web/Services/ProjectService.cs` | Modify | Implement `SavePhasesAsync`, include phases in `GetByIdAsync` |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor` | Modify | TargetLaunchDate field + phase editor section |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs` | Modify | `PhaseEditorModel`, phase list methods, save phases after create |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor` | Modify | TargetLaunchDate field + phase editor section |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor.cs` | Modify | Load existing phases, phase list methods, save phases on submit |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor` | Modify | Project Plan read-only section + TargetLaunchDate in details |
| EF migration | Run | `AddProjectPlanPhases` |

---

## Task 1: PhaseStatus enum + ProjectPlanPhase entity + ClientProject nav prop

**Files:**
- Modify: `src/FellsideDigital.Domain/Enums/Project.cs`
- Create: `src/FellsideDigital.Web/Data/ProjectPlanPhase.cs`
- Modify: `src/FellsideDigital.Web/Data/ClientProject.cs`

- [ ] **Step 1: Add PhaseStatus enum to Project.cs**

Append to the bottom of `src/FellsideDigital.Domain/Enums/Project.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace FellsideDigital.Domain.Enums;

public enum ProjectStatus
{
    Pending,
    [Display(Name = "In Progress")] InProgress,
    Blocked,
    [Display(Name = "On Hold")] OnHold,
    Completed
}

public enum ProjectType
{
    Website,
    Automation
}

public enum PhaseStatus
{
    [Display(Name = "Not Started")] NotStarted,
    [Display(Name = "In Progress")] InProgress,
    Blocked,
    [Display(Name = "On Hold")] OnHold,
    Completed
}
```

- [ ] **Step 2: Create ProjectPlanPhase entity**

Create `src/FellsideDigital.Web/Data/ProjectPlanPhase.cs`:

```csharp
using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ProjectPlanPhase
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public ClientProject Project { get; set; } = null!;

    public int Order { get; set; }

    public string Title { get; set; } = "";
    public string ShortLabel { get; set; } = "";
    public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;

    public DateTime? TargetCompletionDate { get; set; }
    public string? Notes { get; set; }
    public string? ImportantInformation { get; set; }
    public string? Dependencies { get; set; }
    public string? InternalNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 3: Add PlanPhases nav property to ClientProject**

In `src/FellsideDigital.Web/Data/ClientProject.cs`, add after the `StatusUpdates` line:

```csharp
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<ProjectStatusUpdate> StatusUpdates { get; set; } = [];
    public ICollection<ProjectPlanPhase> PlanPhases { get; set; } = [];
```

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Domain/Enums/Project.cs \
        src/FellsideDigital.Web/Data/ProjectPlanPhase.cs \
        src/FellsideDigital.Web/Data/ClientProject.cs
git commit -m "feat: add PhaseStatus enum and ProjectPlanPhase entity"
```

---

## Task 2: DbContext registration + EF migration

**Files:**
- Modify: `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs`
- Run migration command

- [ ] **Step 1: Add DbSet and EF configuration**

In `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs`, add the DbSet after `ProjectStatusUpdates`:

```csharp
        public DbSet<ClientProject> ClientProjects => Set<ClientProject>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<ProjectStatusUpdate> ProjectStatusUpdates => Set<ProjectStatusUpdate>();
        public DbSet<ProjectPlanPhase> ProjectPlanPhases => Set<ProjectPlanPhase>();
        public DbSet<ContactEnquiry> ContactEnquiries => Set<ContactEnquiry>();
```

Then add EF config for `ProjectPlanPhase` inside `OnModelCreating`, after the `ProjectStatusUpdate` block:

```csharp
            builder.Entity<ProjectPlanPhase>(e =>
            {
                e.HasOne(ph => ph.Project)
                    .WithMany(p => p.PlanPhases)
                    .HasForeignKey(ph => ph.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
```

- [ ] **Step 2: Run migration**

From the solution root (where the `.sln` file lives):

```bash
dotnet ef migrations add AddProjectPlanPhases --project src/FellsideDigital.Web --startup-project src/FellsideDigital.Web
```

Expected: new migration file created in `src/FellsideDigital.Web/Migrations/` with `ProjectPlanPhases` table creation.

- [ ] **Step 3: Verify migration looks correct**

The migration `Up()` method should contain something like:

```csharp
migrationBuilder.CreateTable(
    name: "ProjectPlanPhases",
    columns: table => new
    {
        Id = table.Column<Guid>(...),
        ProjectId = table.Column<Guid>(...),
        Order = table.Column<int>(...),
        Title = table.Column<string>(...),
        ShortLabel = table.Column<string>(...),
        Status = table.Column<int>(...),
        TargetCompletionDate = table.Column<DateTime>(nullable: true),
        Notes = table.Column<string>(nullable: true),
        ImportantInformation = table.Column<string>(nullable: true),
        Dependencies = table.Column<string>(nullable: true),
        InternalNotes = table.Column<string>(nullable: true),
        CreatedAt = table.Column<DateTime>(...),
        UpdatedAt = table.Column<DateTime>(...)
    });
```

And a FK with cascade delete to `ClientProjects`.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs \
        src/FellsideDigital.Web/Migrations/
git commit -m "feat: add ProjectPlanPhases table migration"
```

---

## Task 3: BadgeHelpers — phase status styling

**Files:**
- Modify: `src/FellsideDigital.UI/Helpers/BadgeHelpers.cs`

- [ ] **Step 1: Add phase status badge methods**

Add these three methods to the `BadgeHelpers` static class. Also add `using FellsideDigital.Domain.Enums;` if not already present (it is):

```csharp
    public static string PhaseStatusBadge(PhaseStatus s) => s switch
    {
        PhaseStatus.InProgress  => "bg-blue-50 text-blue-700 ring-1 ring-blue-600/20 dark:bg-blue-400/10 dark:text-blue-400",
        PhaseStatus.Completed   => "bg-emerald-50 text-emerald-700 ring-1 ring-emerald-600/20 dark:bg-emerald-400/10 dark:text-emerald-400",
        PhaseStatus.Blocked     => "bg-red-50 text-red-700 ring-1 ring-red-600/20 dark:bg-red-400/10 dark:text-red-400",
        PhaseStatus.OnHold      => "bg-amber-50 text-amber-700 ring-1 ring-amber-600/20 dark:bg-amber-400/10 dark:text-amber-400",
        PhaseStatus.NotStarted  => "bg-slate-100 text-slate-600 ring-1 ring-slate-500/20 dark:bg-white/5 dark:text-neutral-400",
        _                       => ""
    };

    public static string PhaseStatusDotColor(PhaseStatus s) => s switch
    {
        PhaseStatus.InProgress  => "bg-blue-400",
        PhaseStatus.Completed   => "bg-emerald-400",
        PhaseStatus.Blocked     => "bg-red-400",
        PhaseStatus.OnHold      => "bg-amber-400",
        PhaseStatus.NotStarted  => "bg-slate-400 dark:bg-neutral-500",
        _                       => "bg-slate-400"
    };

    public static string PhaseStatusLabel(PhaseStatus s) => s switch
    {
        PhaseStatus.NotStarted  => "Not Started",
        PhaseStatus.InProgress  => "In Progress",
        PhaseStatus.OnHold      => "On Hold",
        _                       => s.ToString()
    };
```

- [ ] **Step 2: Commit**

```bash
git add src/FellsideDigital.UI/Helpers/BadgeHelpers.cs
git commit -m "feat: add PhaseStatus badge helpers"
```

---

## Task 4: Service layer — SavePhasesAsync + phases in GetByIdAsync

**Files:**
- Modify: `src/FellsideDigital.Web/Services/IProjectService.cs`
- Modify: `src/FellsideDigital.Web/Services/ProjectService.cs`

- [ ] **Step 1: Add SavePhasesAsync to IProjectService**

Full updated `src/FellsideDigital.Web/Services/IProjectService.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IProjectService
{
    Task<ClientProject> CreateAsync(ClientProject project, string adminId);
    Task<ClientProject?> GetByIdAsync(Guid id);
    Task<List<ClientProject>> GetAllAsync();
    Task<List<ClientProject>> GetForClientAsync(string clientId);
    Task UpdateAsync(ClientProject project);
    Task DeleteAsync(Guid id);
    Task AddStatusUpdateAsync(Guid projectId, string message, ProjectStatus? newStatus, string adminId);
    Task<List<ProjectStatusUpdate>> GetStatusUpdatesAsync(Guid projectId);
    Task SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases);
}
```

- [ ] **Step 2: Update GetByIdAsync to include phases**

In `src/FellsideDigital.Web/Services/ProjectService.cs`, replace `GetByIdAsync`:

```csharp
    public async Task<ClientProject?> GetByIdAsync(Guid id)
        => await db.ClientProjects
            .Include(p => p.Client)
            .Include(p => p.Invoices)
            .Include(p => p.StatusUpdates.OrderByDescending(u => u.CreatedAt))
                .ThenInclude(u => u.CreatedByAdmin)
            .Include(p => p.PlanPhases.OrderBy(ph => ph.Order))
            .FirstOrDefaultAsync(p => p.Id == id);
```

- [ ] **Step 3: Implement SavePhasesAsync**

Add this method to `ProjectService` before the closing brace:

```csharp
    public async Task SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases)
    {
        var existing = await db.ProjectPlanPhases
            .Where(ph => ph.ProjectId == projectId)
            .ToListAsync();

        db.ProjectPlanPhases.RemoveRange(existing);

        for (int i = 0; i < phases.Count; i++)
        {
            phases[i].Id = Guid.NewGuid();
            phases[i].ProjectId = projectId;
            phases[i].Order = i + 1;
            phases[i].CreatedAt = DateTime.UtcNow;
            phases[i].UpdatedAt = DateTime.UtcNow;
        }

        db.ProjectPlanPhases.AddRange(phases);
        await db.SaveChangesAsync();
    }
```

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Services/IProjectService.cs \
        src/FellsideDigital.Web/Services/ProjectService.cs
git commit -m "feat: add SavePhasesAsync and include phases in GetByIdAsync"
```

---

## Task 5: Create page — TargetLaunchDate + phase editor

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs`
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor`

- [ ] **Step 1: Update Create.razor.cs**

Full replacement of `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Create : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private InputModel Input { get; set; } = new();
    private List<ApplicationUser> _clients = [];
    private List<PhaseEditorModel> _phases = [];
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    protected override async Task OnInitializedAsync()
    {
        var allUsers = UserManager.Users.ToList();
        var adminIds = (await UserManager.GetUsersInRoleAsync("SiteAdmin"))
            .Select(u => u.Id)
            .ToHashSet();
        _clients = allUsers.Where(u => !adminIds.Contains(u.Id)).ToList();
    }

    private void AddPhase()
    {
        if (_phases.Count >= 5) return;
        _phases.Add(new PhaseEditorModel { IsExpanded = true });
    }

    private void RemovePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases.RemoveAt(index);
    }

    private void MovePhaseUp(int index)
    {
        if (index <= 0 || index >= _phases.Count) return;
        (_phases[index - 1], _phases[index]) = (_phases[index], _phases[index - 1]);
    }

    private void MovePhaseDown(int index)
    {
        if (index < 0 || index >= _phases.Count - 1) return;
        (_phases[index], _phases[index + 1]) = (_phases[index + 1], _phases[index]);
    }

    private void TogglePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases[index].IsExpanded = !_phases[index].IsExpanded;
    }

    private void OnPhaseTargetDateChange(int index, string? value)
    {
        _phases[index].TargetCompletionDate = string.IsNullOrEmpty(value)
            ? null
            : DateTime.TryParse(value, out var d) ? (DateTime?)d : null;
    }

    private async Task CreateAsync()
    {
        _submitting = true;
        _errorMessage = null;
        try
        {
            var authState = await AuthState.GetAuthenticationStateAsync();
            var admin = await UserManager.GetUserAsync(authState.User);
            if (admin is null) { _errorMessage = "Could not identify admin user."; return; }

            var project = new ClientProject
            {
                ClientId = Input.ClientId,
                Name = Input.Name,
                Description = Input.Description,
                Type = Input.Type,
                Status = Input.Status,
                TargetLaunchDate = Input.TargetLaunchDate,
                PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim(),
                ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim(),
                DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim()
            };

            await ProjectService.CreateAsync(project, admin.Id);

            if (_phases.Count > 0)
            {
                var phases = _phases.Select(p => new ProjectPlanPhase
                {
                    Title = p.Title.Trim(),
                    ShortLabel = p.ShortLabel.Trim(),
                    Status = p.Status,
                    TargetCompletionDate = p.TargetCompletionDate,
                    Notes = string.IsNullOrWhiteSpace(p.Notes) ? null : p.Notes.Trim(),
                    ImportantInformation = string.IsNullOrWhiteSpace(p.ImportantInformation) ? null : p.ImportantInformation.Trim(),
                    Dependencies = string.IsNullOrWhiteSpace(p.Dependencies) ? null : p.Dependencies.Trim(),
                    InternalNotes = string.IsNullOrWhiteSpace(p.InternalNotes) ? null : p.InternalNotes.Trim()
                }).ToList();

                await ProjectService.SavePhasesAsync(project.Id, phases);
            }

            NavigationManager.NavigateTo($"/Admin/Projects/{project.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create project: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required] public string ClientId { get; set; } = "";
        [Required] public string Name { get; set; } = "";
        [Required] public string Description { get; set; } = "";
        public ProjectType Type { get; set; } = ProjectType.Website;
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
        public DateTime? TargetLaunchDate { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }

    private sealed class PhaseEditorModel
    {
        public string Title { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;
        public DateTime? TargetCompletionDate { get; set; }
        public string? Notes { get; set; }
        public string? ImportantInformation { get; set; }
        public string? Dependencies { get; set; }
        public string? InternalNotes { get; set; }
        public bool IsExpanded { get; set; } = true;
    }
}
```

- [ ] **Step 2: Update Create.razor**

Full replacement of `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor`:

```razor
@page "/Admin/Projects/Create"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout AdminLayout

@using FellsideDigital.Domain.Enums
@using FellsideDigital.Domain.Extensions
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Identity
@using FellsideDigital.Web.Data
@using FellsideDigital.Web.Services
@using FellsideDigital.Web.Components.Layout
@using FellsideDigital.Web.Components.Shared

<PageTitle>New Project — Fellside Digital Admin</PageTitle>

<div class="mb-8">
    <a href="/Admin/Projects"
       class="inline-flex items-center gap-1 text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors mb-3">
        <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
        </svg>
        Projects
    </a>
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">New client project</h1>
    <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">
        Assign a project to a client so they can track progress in their portal.
    </p>
</div>

<AlertBanner Message="@_errorMessage" Variant="error" Class="mb-6" />

<div class="max-w-4xl space-y-6">

    <!-- ── Core details card ── -->
    <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm p-6 sm:p-8">
        <EditForm Model="Input" OnValidSubmit="CreateAsync" class="space-y-6">
            <DataAnnotationsValidator />

            <!-- Section label -->
            <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider -mb-2">Project details</p>

            <!-- Client -->
            <div>
                <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                    Client <span class="text-red-500">*</span>
                </label>
                <InputSelect @bind-Value="Input.ClientId" class="@InputClass">
                    <option value="">Select a client…</option>
                    @foreach (var client in _clients)
                    {
                        <option value="@client.Id">@client.CompanyName — @client.Email</option>
                    }
                </InputSelect>
                <ValidationMessage For="() => Input.ClientId" class="mt-1 text-xs text-red-600 dark:text-red-400" />
            </div>

            <!-- Name -->
            <div>
                <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                    Project name <span class="text-red-500">*</span>
                </label>
                <InputText @bind-Value="Input.Name" placeholder="e.g. Acme Ltd Website" class="@InputClass" />
                <ValidationMessage For="() => Input.Name" class="mt-1 text-xs text-red-600 dark:text-red-400" />
            </div>

            <!-- Description -->
            <div>
                <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                    Description <span class="text-red-500">*</span>
                </label>
                <InputTextArea @bind-Value="Input.Description" rows="3"
                               placeholder="Brief summary shown to the client in their portal…"
                               class="@($"{InputClass} resize-none")" />
                <ValidationMessage For="() => Input.Description" class="mt-1 text-xs text-red-600 dark:text-red-400" />
            </div>

            <!-- Type + Status + Target launch date -->
            <div class="grid grid-cols-1 gap-5 sm:grid-cols-3">
                <!-- Type tab switcher -->
                <div class="sm:col-span-1">
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                        Type <span class="text-red-500">*</span>
                    </label>
                    <div class="flex rounded-xl border border-gray-200 dark:border-white/10 p-1 bg-gray-50 dark:bg-white/5 gap-1 h-[42px]">
                        <button type="button"
                                @onclick='() => Input.Type = ProjectType.Website'
                                class="@($"flex-1 flex items-center justify-center gap-1.5 rounded-lg px-2 py-1.5 text-xs font-semibold transition-all " +
                                         (Input.Type == ProjectType.Website
                                             ? "bg-white dark:bg-neutral-800 text-accent shadow-sm"
                                             : "text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white"))">
                            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M12 21a9.004 9.004 0 0 0 8.716-6.747M12 21a9.004 9.004 0 0 1-8.716-6.747M12 21c2.485 0 4.5-4.03 4.5-9S14.485 3 12 3m0 18c-2.485 0-4.5-4.03-4.5-9S9.515 3 12 3" />
                            </svg>
                            Website
                        </button>
                        <button type="button"
                                @onclick='() => Input.Type = ProjectType.Automation'
                                class="@($"flex-1 flex items-center justify-center gap-1.5 rounded-lg px-2 py-1.5 text-xs font-semibold transition-all " +
                                         (Input.Type == ProjectType.Automation
                                             ? "bg-white dark:bg-neutral-800 text-accent shadow-sm"
                                             : "text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white"))">
                            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z" />
                            </svg>
                            Automation
                        </button>
                    </div>
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">Status</label>
                    <InputSelect @bind-Value="Input.Status" class="@InputClass">
                        @foreach (var opt in EnumExtensions.ToOptions<ProjectStatus>())
                        {
                            <option value="@opt.Value">@opt.Display</option>
                        }
                    </InputSelect>
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                        Target launch date
                        <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">(optional)</span>
                    </label>
                    <InputDate @bind-Value="Input.TargetLaunchDate" class="@InputClass" />
                </div>
            </div>

            <!-- Type-specific deployment fields -->
            <div class="border-t border-gray-100 dark:border-white/5 pt-6 space-y-5">
                <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider">
                    @(Input.Type == ProjectType.Website ? "Website details" : "Deployment") (optional)
                </p>

                @if (Input.Type == ProjectType.Website)
                {
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Railway preview URL
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— shown as live iframe embed in the client portal</span>
                        </label>
                        <InputText @bind-Value="Input.PreviewUrl"
                                   placeholder="https://mysite.up.railway.app"
                                   class="@InputClass" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Live website URL
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— "Visit live site" button in portal (optional)</span>
                        </label>
                        <InputText @bind-Value="Input.ProjectUrl"
                                   placeholder="https://yourclient.co.uk"
                                   class="@InputClass" />
                    </div>
                }
                else
                {
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Live tool URL
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— shown as "Your automation tool" card in portal</span>
                        </label>
                        <InputText @bind-Value="Input.ProjectUrl"
                                   placeholder="https://tool.yourclient.co.uk"
                                   class="@InputClass" />
                    </div>
                }

                <div>
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                        Notes for client
                        <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— shown in their portal</span>
                    </label>
                    <InputTextArea @bind-Value="Input.DeploymentNotes" rows="2"
                                   placeholder="@(Input.Type == ProjectType.Website ? "Any notes about the build or deployment…" : "Notes about the automation setup…")"
                                   class="@($"{InputClass} resize-none")" />
                </div>
            </div>

            <div class="flex items-center justify-end gap-4 pt-2 border-t border-gray-100 dark:border-white/5">
                <a href="/Admin/Projects"
                   class="text-sm font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">
                    Cancel
                </a>
                <SpinnerButton IsLoading="@_submitting" LoadingText="Creating…" Class="px-5">
                    Create @(Input.Type == ProjectType.Website ? "website" : "automation")
                </SpinnerButton>
            </div>
        </EditForm>
    </div>

    <!-- ── Project Plan card ── -->
    <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm">
        <div class="flex items-center justify-between px-6 sm:px-8 py-5 border-b border-gray-100 dark:border-white/5">
            <div>
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project plan</h2>
                <p class="text-xs text-gray-500 dark:text-neutral-400 mt-0.5">Up to 5 phases. Visible to the client in their portal.</p>
            </div>
            @if (_phases.Count < 5)
            {
                <button type="button" @onclick="AddPhase"
                        class="inline-flex items-center gap-1.5 rounded-xl bg-accent/10 hover:bg-accent/20 text-accent px-3.5 py-2 text-xs font-semibold transition-colors">
                    <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                    </svg>
                    Add phase
                </button>
            }
        </div>

        <div class="divide-y divide-gray-100 dark:divide-white/5">
            @if (_phases.Count == 0)
            {
                <div class="px-6 sm:px-8 py-10 text-center">
                    <div class="mx-auto mb-3 flex size-10 items-center justify-center rounded-xl bg-gray-100 dark:bg-white/5">
                        <svg class="size-5 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 0 1 0 3.75H5.625a1.875 1.875 0 0 1 0-3.75Z" />
                        </svg>
                    </div>
                    <p class="text-sm font-medium text-gray-900 dark:text-white">No phases yet</p>
                    <p class="text-xs text-gray-500 dark:text-neutral-400 mt-1">Add phases to give the client a clear project roadmap.</p>
                </div>
            }

            @for (int i = 0; i < _phases.Count; i++)
            {
                var idx = i;
                var phase = _phases[idx];

                <div class="transition-all">
                    <!-- Phase card header -->
                    <div class="flex items-center gap-3 px-6 sm:px-8 py-4">
                        <!-- Order badge -->
                        <span class="flex-none flex size-6 items-center justify-center rounded-full bg-gray-100 dark:bg-white/10 text-xs font-bold text-gray-500 dark:text-neutral-400">
                            @(idx + 1)
                        </span>

                        <!-- Title preview (click to expand) -->
                        <button type="button" @onclick="() => TogglePhase(idx)"
                                class="flex-1 text-left min-w-0">
                            <span class="block text-sm font-semibold text-gray-900 dark:text-white truncate">
                                @(string.IsNullOrWhiteSpace(phase.Title) ? "Untitled phase" : phase.Title)
                            </span>
                            @if (!string.IsNullOrWhiteSpace(phase.ShortLabel))
                            {
                                <span class="block text-xs text-gray-500 dark:text-neutral-400 truncate">@phase.ShortLabel</span>
                            }
                        </button>

                        <!-- Status badge -->
                        <span class="hidden sm:inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium @BadgeHelpers.PhaseStatusBadge(phase.Status)">
                            <span class="size-1.5 rounded-full @BadgeHelpers.PhaseStatusDotColor(phase.Status)"></span>
                            @BadgeHelpers.PhaseStatusLabel(phase.Status)
                        </span>

                        <!-- Reorder buttons -->
                        <div class="flex gap-1">
                            <button type="button" @onclick="() => MovePhaseUp(idx)" disabled="@(idx == 0)"
                                    class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                                    title="Move up">
                                <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 15.75l7.5-7.5 7.5 7.5" />
                                </svg>
                            </button>
                            <button type="button" @onclick="() => MovePhaseDown(idx)" disabled="@(idx == _phases.Count - 1)"
                                    class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                                    title="Move down">
                                <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                                </svg>
                            </button>
                        </div>

                        <!-- Delete -->
                        <button type="button" @onclick="() => RemovePhase(idx)"
                                class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-red-500 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-400/10 transition-colors"
                                title="Remove phase">
                            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                            </svg>
                        </button>

                        <!-- Expand chevron -->
                        <button type="button" @onclick="() => TogglePhase(idx)"
                                class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 transition-colors">
                            <svg class="size-3.5 transition-transform @(phase.IsExpanded ? "rotate-180" : "")"
                                 fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                            </svg>
                        </button>
                    </div>

                    <!-- Phase card body (collapsible) -->
                    @if (phase.IsExpanded)
                    {
                        <div class="px-6 sm:px-8 pb-6 space-y-5 border-t border-gray-100 dark:border-white/5 pt-5">

                            <!-- Required fields -->
                            <div class="grid grid-cols-1 gap-5 sm:grid-cols-3">
                                <div class="sm:col-span-1">
                                    <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">
                                        Phase title <span class="text-red-500">*</span>
                                    </label>
                                    <input type="text" @bind="_phases[idx].Title"
                                           placeholder="e.g. Discovery & Planning"
                                           class="@InputClass" />
                                </div>
                                <div>
                                    <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">
                                        Short label <span class="text-red-500">*</span>
                                    </label>
                                    <input type="text" @bind="_phases[idx].ShortLabel"
                                           placeholder="e.g. Discovery"
                                           class="@InputClass" />
                                </div>
                                <div>
                                    <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Status</label>
                                    <select @bind="_phases[idx].Status" class="@InputClass">
                                        @foreach (var opt in EnumExtensions.ToOptions<PhaseStatus>())
                                        {
                                            <option value="@opt.Value">@opt.Display</option>
                                        }
                                    </select>
                                </div>
                            </div>

                            <!-- Optional fields -->
                            <details class="group">
                                <summary class="cursor-pointer text-xs font-semibold text-gray-500 dark:text-neutral-400 hover:text-gray-700 dark:hover:text-white transition-colors select-none list-none flex items-center gap-1.5">
                                    <svg class="size-3.5 transition-transform group-open:rotate-90" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
                                    </svg>
                                    Optional fields
                                </summary>
                                <div class="mt-4 space-y-4">
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Target completion date</label>
                                        <input type="date"
                                               value="@(_phases[idx].TargetCompletionDate?.ToString("yyyy-MM-dd"))"
                                               @onchange="e => OnPhaseTargetDateChange(idx, e.Value?.ToString())"
                                               class="@InputClass" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Notes / description</label>
                                        <textarea @bind="_phases[idx].Notes" rows="2"
                                                  placeholder="What happens in this phase?"
                                                  class="@($"{InputClass} resize-none")"></textarea>
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Important information</label>
                                        <textarea @bind="_phases[idx].ImportantInformation" rows="2"
                                                  placeholder="Key things the client should know…"
                                                  class="@($"{InputClass} resize-none")"></textarea>
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Dependencies / blockers</label>
                                        <input type="text" @bind="_phases[idx].Dependencies"
                                               placeholder="e.g. Awaiting client content, sign-off required"
                                               class="@InputClass" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Internal admin notes</label>
                                        <textarea @bind="_phases[idx].InternalNotes" rows="2"
                                                  placeholder="Not visible to the client…"
                                                  class="@($"{InputClass} resize-none")"></textarea>
                                    </div>
                                </div>
                            </details>
                        </div>
                    }
                </div>
            }

            @if (_phases.Count >= 5)
            {
                <div class="px-6 sm:px-8 py-3 text-center">
                    <p class="text-xs text-gray-400 dark:text-neutral-500">Maximum of 5 phases reached.</p>
                </div>
            }
        </div>
    </div>
</div>
```

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Create.razor.cs
git commit -m "feat: add TargetLaunchDate and phase plan editor to Create page"
```

---

## Task 6: Edit page — TargetLaunchDate + phase editor (loads existing phases)

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor.cs`
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor`

- [ ] **Step 1: Update Edit.razor.cs**

Full replacement of `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Edit : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private InputModel Input { get; set; } = new();
    private List<PhaseEditorModel> _phases = [];
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    protected override async Task OnInitializedAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is not null)
        {
            Input.Name = _project.Name;
            Input.Description = _project.Description;
            Input.Type = _project.Type;
            Input.Status = _project.Status;
            Input.TargetLaunchDate = _project.TargetLaunchDate;
            Input.PreviewUrl = _project.PreviewUrl;
            Input.ProjectUrl = _project.ProjectUrl;
            Input.DeploymentNotes = _project.DeploymentNotes;

            _phases = _project.PlanPhases
                .OrderBy(ph => ph.Order)
                .Select(ph => new PhaseEditorModel
                {
                    Title = ph.Title,
                    ShortLabel = ph.ShortLabel,
                    Status = ph.Status,
                    TargetCompletionDate = ph.TargetCompletionDate,
                    Notes = ph.Notes,
                    ImportantInformation = ph.ImportantInformation,
                    Dependencies = ph.Dependencies,
                    InternalNotes = ph.InternalNotes,
                    IsExpanded = false
                })
                .ToList();
        }
    }

    private void AddPhase()
    {
        if (_phases.Count >= 5) return;
        _phases.Add(new PhaseEditorModel { IsExpanded = true });
    }

    private void RemovePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases.RemoveAt(index);
    }

    private void MovePhaseUp(int index)
    {
        if (index <= 0 || index >= _phases.Count) return;
        (_phases[index - 1], _phases[index]) = (_phases[index], _phases[index - 1]);
    }

    private void MovePhaseDown(int index)
    {
        if (index < 0 || index >= _phases.Count - 1) return;
        (_phases[index], _phases[index + 1]) = (_phases[index + 1], _phases[index]);
    }

    private void TogglePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases[index].IsExpanded = !_phases[index].IsExpanded;
    }

    private void OnPhaseTargetDateChange(int index, string? value)
    {
        _phases[index].TargetCompletionDate = string.IsNullOrEmpty(value)
            ? null
            : DateTime.TryParse(value, out var d) ? (DateTime?)d : null;
    }

    private async Task SaveAsync()
    {
        if (_project is null) return;
        _submitting = true;
        _errorMessage = null;
        try
        {
            _project.Name = Input.Name;
            _project.Description = Input.Description;
            _project.Type = Input.Type;
            _project.Status = Input.Status;
            _project.TargetLaunchDate = Input.TargetLaunchDate;
            _project.PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim();
            _project.ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim();
            _project.DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim();

            await ProjectService.UpdateAsync(_project);

            var phases = _phases.Select(p => new ProjectPlanPhase
            {
                Title = p.Title.Trim(),
                ShortLabel = p.ShortLabel.Trim(),
                Status = p.Status,
                TargetCompletionDate = p.TargetCompletionDate,
                Notes = string.IsNullOrWhiteSpace(p.Notes) ? null : p.Notes.Trim(),
                ImportantInformation = string.IsNullOrWhiteSpace(p.ImportantInformation) ? null : p.ImportantInformation.Trim(),
                Dependencies = string.IsNullOrWhiteSpace(p.Dependencies) ? null : p.Dependencies.Trim(),
                InternalNotes = string.IsNullOrWhiteSpace(p.InternalNotes) ? null : p.InternalNotes.Trim()
            }).ToList();

            await ProjectService.SavePhasesAsync(_project.Id, phases);

            NavigationManager.NavigateTo($"/Admin/Projects/{Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required] public string Name { get; set; } = "";
        [Required] public string Description { get; set; } = "";
        public ProjectType Type { get; set; } = ProjectType.Website;
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
        public DateTime? TargetLaunchDate { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }

    private sealed class PhaseEditorModel
    {
        public string Title { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;
        public DateTime? TargetCompletionDate { get; set; }
        public string? Notes { get; set; }
        public string? ImportantInformation { get; set; }
        public string? Dependencies { get; set; }
        public string? InternalNotes { get; set; }
        public bool IsExpanded { get; set; } = true;
    }
}
```

- [ ] **Step 2: Update Edit.razor**

Full replacement of `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor`:

```razor
@page "/Admin/Projects/{Id:guid}/Edit"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout AdminLayout

@using FellsideDigital.Domain.Enums
@using FellsideDigital.Domain.Extensions
@using Microsoft.AspNetCore.Authorization
@using FellsideDigital.Web.Data
@using FellsideDigital.Web.Services
@using FellsideDigital.Web.Components.Layout
@using FellsideDigital.Web.Components.Shared

<PageTitle>Edit Project — Fellside Digital Admin</PageTitle>

<div class="mb-8">
    <a href="@($"/Admin/Projects/{Id}")"
       class="inline-flex items-center gap-1 text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors mb-3">
        <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
        </svg>
        Back to project
    </a>
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">Edit project</h1>
    <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">Update project details, timeline, and phase plan.</p>
</div>

@if (_project is null)
{
    <div class="animate-pulse space-y-4 max-w-4xl">
        <div class="rounded-2xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 h-48"></div>
        <div class="rounded-2xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 h-64"></div>
    </div>
}
else
{
    <AlertBanner Message="@_errorMessage" Variant="error" Class="mb-6" />

    <div class="max-w-4xl space-y-6">

        <!-- ── Core details card ── -->
        <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm p-6 sm:p-8">
            <EditForm Model="Input" OnValidSubmit="SaveAsync" class="space-y-6">
                <DataAnnotationsValidator />

                <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider -mb-2">Project details</p>

                <div>
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                        Project name <span class="text-red-500">*</span>
                    </label>
                    <InputText @bind-Value="Input.Name" class="@InputClass" />
                    <ValidationMessage For="() => Input.Name" class="mt-1 text-xs text-red-600 dark:text-red-400" />
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                        Description <span class="text-red-500">*</span>
                    </label>
                    <InputTextArea @bind-Value="Input.Description" rows="3" class="@($"{InputClass} resize-none")" />
                    <ValidationMessage For="() => Input.Description" class="mt-1 text-xs text-red-600 dark:text-red-400" />
                </div>

                <div class="grid grid-cols-1 gap-5 sm:grid-cols-3">
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">Type</label>
                        <InputSelect @bind-Value="Input.Type" class="@InputClass">
                            <option value="@ProjectType.Website">Website</option>
                            <option value="@ProjectType.Automation">Automation</option>
                        </InputSelect>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">Status</label>
                        <InputSelect @bind-Value="Input.Status" class="@InputClass">
                            @foreach (var options in EnumExtensions.ToOptions<ProjectStatus>())
                            {
                                <option value="@options.Value">@options.Display</option>
                            }
                        </InputSelect>
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Target launch date
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">(optional)</span>
                        </label>
                        <InputDate @bind-Value="Input.TargetLaunchDate" class="@InputClass" />
                    </div>
                </div>

                <div class="border-t border-gray-100 dark:border-white/5 pt-6 space-y-5">
                    <p class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider">Deployment (optional)</p>

                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Railway preview URL
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— iframe embed</span>
                        </label>
                        <InputText @bind-Value="Input.PreviewUrl" placeholder="https://myapp.up.railway.app" class="@InputClass" />
                    </div>

                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">
                            Project URL
                            <span class="ml-1 text-xs font-normal text-gray-400 dark:text-neutral-500">— external link</span>
                        </label>
                        <InputText @bind-Value="Input.ProjectUrl" placeholder="https://railway.app/project/..." class="@InputClass" />
                    </div>

                    <div>
                        <label class="block text-sm font-medium text-gray-900 dark:text-white mb-1.5">Deployment notes</label>
                        <InputTextArea @bind-Value="Input.DeploymentNotes" rows="2" class="@($"{InputClass} resize-none")" />
                    </div>
                </div>

                <div class="flex items-center justify-end gap-4 pt-2 border-t border-gray-100 dark:border-white/5">
                    <a href="@($"/Admin/Projects/{Id}")"
                       class="text-sm font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">
                        Cancel
                    </a>
                    <SpinnerButton IsLoading="@_submitting" LoadingText="Saving…" Class="px-5">
                        Save changes
                    </SpinnerButton>
                </div>
            </EditForm>
        </div>

        <!-- ── Project Plan card ── -->
        <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm">
            <div class="flex items-center justify-between px-6 sm:px-8 py-5 border-b border-gray-100 dark:border-white/5">
                <div>
                    <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project plan</h2>
                    <p class="text-xs text-gray-500 dark:text-neutral-400 mt-0.5">Up to 5 phases. Saved together with project details above.</p>
                </div>
                @if (_phases.Count < 5)
                {
                    <button type="button" @onclick="AddPhase"
                            class="inline-flex items-center gap-1.5 rounded-xl bg-accent/10 hover:bg-accent/20 text-accent px-3.5 py-2 text-xs font-semibold transition-colors">
                        <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                        </svg>
                        Add phase
                    </button>
                }
            </div>

            <div class="divide-y divide-gray-100 dark:divide-white/5">
                @if (_phases.Count == 0)
                {
                    <div class="px-6 sm:px-8 py-10 text-center">
                        <div class="mx-auto mb-3 flex size-10 items-center justify-center rounded-xl bg-gray-100 dark:bg-white/5">
                            <svg class="size-5 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 12h16.5m-16.5 3.75h16.5M3.75 19.5h16.5M5.625 4.5h12.75a1.875 1.875 0 0 1 0 3.75H5.625a1.875 1.875 0 0 1 0-3.75Z" />
                            </svg>
                        </div>
                        <p class="text-sm font-medium text-gray-900 dark:text-white">No phases yet</p>
                        <p class="text-xs text-gray-500 dark:text-neutral-400 mt-1">Add phases to give the client a clear project roadmap.</p>
                    </div>
                }

                @for (int i = 0; i < _phases.Count; i++)
                {
                    var idx = i;
                    var phase = _phases[idx];

                    <div class="transition-all">
                        <div class="flex items-center gap-3 px-6 sm:px-8 py-4">
                            <span class="flex-none flex size-6 items-center justify-center rounded-full bg-gray-100 dark:bg-white/10 text-xs font-bold text-gray-500 dark:text-neutral-400">
                                @(idx + 1)
                            </span>

                            <button type="button" @onclick="() => TogglePhase(idx)" class="flex-1 text-left min-w-0">
                                <span class="block text-sm font-semibold text-gray-900 dark:text-white truncate">
                                    @(string.IsNullOrWhiteSpace(phase.Title) ? "Untitled phase" : phase.Title)
                                </span>
                                @if (!string.IsNullOrWhiteSpace(phase.ShortLabel))
                                {
                                    <span class="block text-xs text-gray-500 dark:text-neutral-400 truncate">@phase.ShortLabel</span>
                                }
                            </button>

                            <span class="hidden sm:inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-medium @BadgeHelpers.PhaseStatusBadge(phase.Status)">
                                <span class="size-1.5 rounded-full @BadgeHelpers.PhaseStatusDotColor(phase.Status)"></span>
                                @BadgeHelpers.PhaseStatusLabel(phase.Status)
                            </span>

                            <div class="flex gap-1">
                                <button type="button" @onclick="() => MovePhaseUp(idx)" disabled="@(idx == 0)"
                                        class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                                        title="Move up">
                                    <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 15.75l7.5-7.5 7.5 7.5" />
                                    </svg>
                                </button>
                                <button type="button" @onclick="() => MovePhaseDown(idx)" disabled="@(idx == _phases.Count - 1)"
                                        class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                                        title="Move down">
                                    <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                                    </svg>
                                </button>
                            </div>

                            <button type="button" @onclick="() => RemovePhase(idx)"
                                    class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-red-500 dark:hover:text-red-400 hover:bg-red-50 dark:hover:bg-red-400/10 transition-colors"
                                    title="Remove phase">
                                <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>

                            <button type="button" @onclick="() => TogglePhase(idx)"
                                    class="p-1.5 rounded-lg text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white hover:bg-gray-100 dark:hover:bg-white/10 transition-colors">
                                <svg class="size-3.5 transition-transform @(phase.IsExpanded ? "rotate-180" : "")"
                                     fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                                </svg>
                            </button>
                        </div>

                        @if (phase.IsExpanded)
                        {
                            <div class="px-6 sm:px-8 pb-6 space-y-5 border-t border-gray-100 dark:border-white/5 pt-5">
                                <div class="grid grid-cols-1 gap-5 sm:grid-cols-3">
                                    <div class="sm:col-span-1">
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">
                                            Phase title <span class="text-red-500">*</span>
                                        </label>
                                        <input type="text" @bind="_phases[idx].Title"
                                               placeholder="e.g. Discovery & Planning"
                                               class="@InputClass" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">
                                            Short label <span class="text-red-500">*</span>
                                        </label>
                                        <input type="text" @bind="_phases[idx].ShortLabel"
                                               placeholder="e.g. Discovery"
                                               class="@InputClass" />
                                    </div>
                                    <div>
                                        <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Status</label>
                                        <select @bind="_phases[idx].Status" class="@InputClass">
                                            @foreach (var opt in EnumExtensions.ToOptions<PhaseStatus>())
                                            {
                                                <option value="@opt.Value">@opt.Display</option>
                                            }
                                        </select>
                                    </div>
                                </div>

                                <details class="group">
                                    <summary class="cursor-pointer text-xs font-semibold text-gray-500 dark:text-neutral-400 hover:text-gray-700 dark:hover:text-white transition-colors select-none list-none flex items-center gap-1.5">
                                        <svg class="size-3.5 transition-transform group-open:rotate-90" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                            <path stroke-linecap="round" stroke-linejoin="round" d="m8.25 4.5 7.5 7.5-7.5 7.5" />
                                        </svg>
                                        Optional fields
                                    </summary>
                                    <div class="mt-4 space-y-4">
                                        <div>
                                            <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Target completion date</label>
                                            <input type="date"
                                                   value="@(_phases[idx].TargetCompletionDate?.ToString("yyyy-MM-dd"))"
                                                   @onchange="e => OnPhaseTargetDateChange(idx, e.Value?.ToString())"
                                                   class="@InputClass" />
                                        </div>
                                        <div>
                                            <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Notes / description</label>
                                            <textarea @bind="_phases[idx].Notes" rows="2"
                                                      placeholder="What happens in this phase?"
                                                      class="@($"{InputClass} resize-none")"></textarea>
                                        </div>
                                        <div>
                                            <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Important information</label>
                                            <textarea @bind="_phases[idx].ImportantInformation" rows="2"
                                                      placeholder="Key things the client should know…"
                                                      class="@($"{InputClass} resize-none")"></textarea>
                                        </div>
                                        <div>
                                            <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Dependencies / blockers</label>
                                            <input type="text" @bind="_phases[idx].Dependencies"
                                                   placeholder="e.g. Awaiting client content, sign-off required"
                                                   class="@InputClass" />
                                        </div>
                                        <div>
                                            <label class="block text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1.5">Internal admin notes</label>
                                            <textarea @bind="_phases[idx].InternalNotes" rows="2"
                                                      placeholder="Not visible to the client…"
                                                      class="@($"{InputClass} resize-none")"></textarea>
                                        </div>
                                    </div>
                                </details>
                            </div>
                        }
                    </div>
                }

                @if (_phases.Count >= 5)
                {
                    <div class="px-6 sm:px-8 py-3 text-center">
                        <p class="text-xs text-gray-400 dark:text-neutral-500">Maximum of 5 phases reached.</p>
                    </div>
                }
            </div>
        </div>
    </div>
}
```

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Edit.razor.cs
git commit -m "feat: add TargetLaunchDate and phase plan editor to Edit page"
```

---

## Task 7: Detail page — read-only plan section + TargetLaunchDate display

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor`
- No change to `Detail.razor.cs` — `GetByIdAsync` now includes phases automatically.

- [ ] **Step 1: Add TargetLaunchDate to the project details dl**

In `Detail.razor`, in the `<dl class="divide-y ...">` inside the "Project details" `CardSection`, add a TargetLaunchDate row in the first `<div class="grid grid-cols-2">` block. Replace that block:

```razor
                <dl class="divide-y divide-gray-100 dark:divide-white/5">
                    <div class="grid grid-cols-2 sm:grid-cols-@(_project.TargetLaunchDate.HasValue ? "3" : "2")">
                        <div class="px-6 py-4">
                            <dt class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1">Type</dt>
                            <dd class="text-sm text-gray-900 dark:text-white">@_project.Type</dd>
                        </div>
                        <div class="px-6 py-4 border-l border-gray-100 dark:border-white/5">
                            <dt class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1">Created</dt>
                            <dd class="text-sm text-gray-900 dark:text-white">@_project.CreatedAt.ToLocalTime().ToString("d MMM yyyy")</dd>
                        </div>
                        @if (_project.TargetLaunchDate.HasValue)
                        {
                            <div class="px-6 py-4 border-l border-gray-100 dark:border-white/5">
                                <dt class="text-xs font-semibold text-gray-500 dark:text-neutral-400 uppercase tracking-wider mb-1">Target launch</dt>
                                <dd class="text-sm text-gray-900 dark:text-white">@_project.TargetLaunchDate.Value.ToLocalTime().ToString("d MMM yyyy")</dd>
                            </div>
                        }
                    </div>
```

- [ ] **Step 2: Add Project Plan section**

In `Detail.razor`, inside the left column (`<div class="lg:col-span-2 space-y-6">`), add the Project Plan section **after** the "Project details" `CardSection` and **before** the "Post status update" div:

```razor
            <!-- Project plan -->
            @if (_project.PlanPhases.Any())
            {
                <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm overflow-hidden">
                    <div class="px-6 py-5 border-b border-gray-100 dark:border-white/5 flex items-center justify-between">
                        <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project plan</h2>
                        <span class="text-xs text-gray-400 dark:text-neutral-500">@_project.PlanPhases.Count phase@(_project.PlanPhases.Count == 1 ? "" : "s")</span>
                    </div>

                    <!-- Progress bar -->
                    @{
                        var completedCount = _project.PlanPhases.Count(ph => ph.Status == PhaseStatus.Completed);
                        var progressPct = _project.PlanPhases.Count > 0
                            ? (int)Math.Round((double)completedCount / _project.PlanPhases.Count * 100)
                            : 0;
                    }
                    <div class="px-6 py-3 border-b border-gray-100 dark:border-white/5">
                        <div class="flex items-center justify-between mb-1.5">
                            <span class="text-xs text-gray-500 dark:text-neutral-400">@completedCount of @_project.PlanPhases.Count phases complete</span>
                            <span class="text-xs font-semibold text-gray-900 dark:text-white">@progressPct%</span>
                        </div>
                        <div class="h-1.5 w-full rounded-full bg-gray-100 dark:bg-white/10">
                            <div class="h-1.5 rounded-full bg-accent transition-all" style="width: @progressPct%"></div>
                        </div>
                    </div>

                    <!-- Phase list -->
                    <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                        @foreach (var phase in _project.PlanPhases.OrderBy(ph => ph.Order))
                        {
                            <li class="px-6 py-4">
                                <div class="flex items-start gap-3">
                                    <!-- Order + status dot -->
                                    <div class="flex-none flex size-7 items-center justify-center rounded-full bg-gray-100 dark:bg-white/10 mt-0.5">
                                        @if (phase.Status == PhaseStatus.Completed)
                                        {
                                            <svg class="size-3.5 text-emerald-500" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                                            </svg>
                                        }
                                        else
                                        {
                                            <span class="text-xs font-bold text-gray-400 dark:text-neutral-500">@phase.Order</span>
                                        }
                                    </div>

                                    <div class="flex-1 min-w-0">
                                        <div class="flex flex-wrap items-center gap-2 mb-0.5">
                                            <span class="text-sm font-semibold text-gray-900 dark:text-white">@phase.Title</span>
                                            <span class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-0.5 text-xs font-medium @BadgeHelpers.PhaseStatusBadge(phase.Status)">
                                                <span class="size-1.5 rounded-full @BadgeHelpers.PhaseStatusDotColor(phase.Status)"></span>
                                                @BadgeHelpers.PhaseStatusLabel(phase.Status)
                                            </span>
                                        </div>

                                        @if (!string.IsNullOrWhiteSpace(phase.ShortLabel))
                                        {
                                            <span class="text-xs text-gray-400 dark:text-neutral-500">@phase.ShortLabel</span>
                                        }

                                        @if (!string.IsNullOrWhiteSpace(phase.Notes))
                                        {
                                            <p class="mt-1.5 text-sm text-gray-600 dark:text-neutral-300 leading-relaxed">@phase.Notes</p>
                                        }

                                        @if (!string.IsNullOrWhiteSpace(phase.ImportantInformation))
                                        {
                                            <div class="mt-2 flex items-start gap-1.5 rounded-lg bg-amber-50 dark:bg-amber-400/10 px-3 py-2">
                                                <svg class="size-3.5 text-amber-500 flex-none mt-0.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                                    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" />
                                                </svg>
                                                <p class="text-xs text-amber-700 dark:text-amber-400">@phase.ImportantInformation</p>
                                            </div>
                                        }

                                        @if (!string.IsNullOrWhiteSpace(phase.Dependencies))
                                        {
                                            <p class="mt-1.5 text-xs text-gray-400 dark:text-neutral-500">
                                                <span class="font-semibold">Blockers:</span> @phase.Dependencies
                                            </p>
                                        }

                                        @if (phase.TargetCompletionDate.HasValue)
                                        {
                                            <p class="mt-1 text-xs text-gray-400 dark:text-neutral-500">
                                                Target: @phase.TargetCompletionDate.Value.ToLocalTime().ToString("d MMM yyyy")
                                            </p>
                                        }

                                        @if (!string.IsNullOrWhiteSpace(phase.InternalNotes))
                                        {
                                            <div class="mt-2 rounded-lg border border-dashed border-gray-200 dark:border-white/10 px-3 py-2">
                                                <p class="text-xs text-gray-400 dark:text-neutral-500">
                                                    <span class="font-semibold uppercase tracking-wider">Admin only:</span> @phase.InternalNotes
                                                </p>
                                            </div>
                                        }
                                    </div>
                                </div>
                            </li>
                        }
                    </ul>
                </div>
            }
```

You also need to add `@using FellsideDigital.Domain.Enums` if not already in `Detail.razor` — it is already present.

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor
git commit -m "feat: add project plan read-only view and TargetLaunchDate to Detail page"
```

---

## Self-Review

**Spec coverage:**
- ✅ ProjectEndDate → using existing `TargetLaunchDate` field; datepicker added to Create + Edit
- ✅ Up to 5 phases per project, ordered, editable
- ✅ Add / remove / reorder (up/down buttons) in admin UI
- ✅ PhaseStatus: NotStarted, InProgress, Blocked, OnHold, Completed
- ✅ Phase fields: Title, ShortLabel, Status (required), TargetCompletionDate, Notes, ImportantInformation, Dependencies, InternalNotes (optional)
- ✅ Status badges visually distinct (color per status via BadgeHelpers)
- ✅ Card layout with collapsible optional fields
- ✅ Empty state in phase editor
- ✅ Max 5 guard in UI + AddPhase method
- ✅ Future-proof: separate table, ordered by `Order` int, serialisable, client portal ready
- ✅ Detail page: read-only phase list, progress bar, TargetLaunchDate display
- ✅ Cascade delete: phases deleted when project deleted

**Type consistency check:**
- `PhaseEditorModel` defined identically in both Create and Edit code-behind ✅
- `PhaseStatus` enum used consistently across BadgeHelpers, PhaseEditorModel, ProjectPlanPhase ✅
- `SavePhasesAsync(Guid projectId, List<ProjectPlanPhase> phases)` signature matches interface + implementation ✅
- `_project.PlanPhases` navigation property name matches entity definition ✅

**Placeholder scan:** No TBDs, no incomplete steps. All code is complete. ✅
