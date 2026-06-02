# Client Project Page Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the client-facing project page into a calm, dashboard-style view (stat bar → big preview → project plan + documents + invoices) driven by existing phase data, and add an admin-uploadable project Documents feature (proposal + shared files) reusing the S3 storage pattern.

**Architecture:** New `ProjectDocument` entity + `IProjectDocumentService` mirror the existing `Invoice`/`InvoiceService` S3 pattern (`IStorageService` + presigned URLs). The shared `PortalProjectDetailView.razor` is rewritten; a new admin `Documents` page mirrors the existing admin `Notes` page. Document uploads also record a `DocumentShared` timeline event (enum value already reserved).

**Tech Stack:** ASP.NET Core / Blazor Server (Interactive Server), EF Core + PostgreSQL, Tailwind CSS, AWS S3 (`IStorageService`).

**Conventions for every task (no test projects in this solution):**
- Build/verify in WSL with `dotnet.exe` (not `dotnet`). Build only the Web project:
  `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
- A CS0103 error mentioning `Html` in `App.razor` is a known flaky source-generator artifact — ignore it if it appears; any *other* error is real.
- Commit after each task.

---

## File Structure

- Create `src/FellsideDigital.Web/Data/ProjectDocument.cs` — the entity.
- Modify `src/FellsideDigital.Web/Data/ClientProject.cs` — add `Documents` collection.
- Modify `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs` — DbSet + relationship config.
- Create migration `…_AddProjectDocuments.cs` (EF-generated).
- Create `src/FellsideDigital.Web/Services/IProjectDocumentService.cs` + `ProjectDocumentService.cs`.
- Modify `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs` — register service.
- Modify `src/FellsideDigital.Web/Services/ProjectService.cs` — include `Documents` in two getters.
- Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor` + `.razor.cs`.
- Modify `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor` — link to Documents.
- Modify `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs` — load document URLs.
- Rewrite `src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor` — the new layout.

---

### Task 1: `ProjectDocument` entity + `ClientProject.Documents` + DbContext config

**Files:**
- Create: `src/FellsideDigital.Web/Data/ProjectDocument.cs`
- Modify: `src/FellsideDigital.Web/Data/ClientProject.cs:32`
- Modify: `src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs` (DbSet near line 15; config after the `ProjectPlanPhase` block ~line 179)

- [ ] **Step 1: Create the entity**

```csharp
namespace FellsideDigital.Web.Data;

public class ProjectDocument
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";  // S3 object key — not a web URL
    public string FileName { get; set; } = "";  // original filename for display

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Add the navigation collection to `ClientProject`**

In `ClientProject.cs`, directly under the existing `PlanPhases` line (`:32`), add:

```csharp
    public ICollection<ProjectDocument> Documents { get; set; } = [];
```

- [ ] **Step 3: Add the DbSet**

In `FellsideDigitalDbContext.cs`, under the `ProjectPlanPhases` DbSet line (~`:15`), add:

```csharp
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();
```

- [ ] **Step 4: Add the relationship config**

In `OnModelCreating`, immediately after the `builder.Entity<ProjectPlanPhase>(…)` block, add:

```csharp
            builder.Entity<ProjectDocument>(e =>
            {
                e.HasOne(d => d.Project)
                    .WithMany(p => p.Documents)
                    .HasForeignKey(d => d.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
```

- [ ] **Step 5: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded (ignore any `Html`/`App.razor` CS0103 artifact).

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/Data/ProjectDocument.cs src/FellsideDigital.Web/Data/ClientProject.cs src/FellsideDigital.Web/Data/FellsideDigitalDbContext.cs
git commit -m "feat: add ProjectDocument entity and relationship"
```

---

### Task 2: EF migration `AddProjectDocuments`

**Files:**
- Create: `src/FellsideDigital.Web/Data/Migrations/…_AddProjectDocuments.cs` (+ Designer + snapshot update, all EF-generated)

- [ ] **Step 1: Generate the migration**

Run:
```bash
dotnet.exe ef migrations add AddProjectDocuments \
  --project src/FellsideDigital.Web/FellsideDigital.Web.csproj \
  --startup-project src/FellsideDigital.Web/FellsideDigital.Web.csproj
```
Expected: "Done." and a new `…_AddProjectDocuments.cs` under `Data/Migrations/`.

- [ ] **Step 2: Sanity-check the migration**

Open the generated `…_AddProjectDocuments.cs`. Confirm `Up()` calls `migrationBuilder.CreateTable(name: "ProjectDocuments", …)` with `ProjectId`, `Title`, `FilePath`, `FileName`, `CreatedAt`, and a FK to `ClientProjects` with `onDelete: ReferentialAction.Cascade`. No other tables should be touched.

- [ ] **Step 3: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Data/Migrations
git commit -m "feat: migration for ProjectDocuments table"
```

(Migrations apply automatically on startup — no manual `database update` needed.)

---

### Task 3: `IProjectDocumentService` + `ProjectDocumentService` + DI

**Files:**
- Create: `src/FellsideDigital.Web/Services/IProjectDocumentService.cs`
- Create: `src/FellsideDigital.Web/Services/ProjectDocumentService.cs`
- Modify: `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs:95`

- [ ] **Step 1: Create the interface**

```csharp
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Services;

public interface IProjectDocumentService
{
    Task<ProjectDocument> UploadAsync(Guid projectId, string title, IBrowserFile file, string? actorId = null);
    Task<List<ProjectDocument>> GetForProjectAsync(Guid projectId);
    Task DeleteAsync(Guid id);

    /// <summary>Time-limited presigned download URL for the document file, or null if missing.</summary>
    Task<string?> GetDownloadUrlAsync(Guid id);
}
```

- [ ] **Step 2: Create the implementation** (mirrors `InvoiceService`)

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class ProjectDocumentService(
    FellsideDigitalDbContext db,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    IProjectTimelineService timeline) : IProjectDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"];

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"]  = "application/pdf",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".doc"]  = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    };

    public async Task<ProjectDocument> UploadAsync(Guid projectId, string title, IBrowserFile file, string? actorId = null)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use PDF, Word, or an image.");

        var documentId = Guid.NewGuid();
        var key = $"documents/{projectId}/{documentId}{ext}";
        var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

        await using var stream = file.OpenReadStream(maxAllowedSize: 25 * 1024 * 1024);
        await storage.UploadAsync(key, stream, contentType);

        var document = new ProjectDocument
        {
            Id        = documentId,
            ProjectId = projectId,
            Title     = title,
            FilePath  = key,
            FileName  = file.Name,
            CreatedAt = DateTime.UtcNow,
        };

        db.ProjectDocuments.Add(document);
        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            projectId, TimelineEventType.DocumentShared, $"Document shared: {title}",
            TimelineVisibility.ClientVisible, actorId, occurredAt: document.CreatedAt);

        return document;
    }

    public async Task<List<ProjectDocument>> GetForProjectAsync(Guid projectId)
        => await db.ProjectDocuments
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<string?> GetDownloadUrlAsync(Guid id)
    {
        var document = await db.ProjectDocuments.FindAsync(id);
        if (document is null || string.IsNullOrEmpty(document.FilePath)) return null;

        var expiry = TimeSpan.FromMinutes(storageOptions.Value.PresignedUrlExpiryMinutes);
        return await storage.GetPresignedUrlAsync(document.FilePath, expiry);
    }

    public async Task DeleteAsync(Guid id)
    {
        var document = await db.ProjectDocuments.FindAsync(id);
        if (document is null) return;

        if (!string.IsNullOrEmpty(document.FilePath))
        {
            try { await storage.DeleteAsync(document.FilePath); }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"S3 delete failed for {document.FilePath}: {ex.Message}");
            }
        }

        db.ProjectDocuments.Remove(document);
        await db.SaveChangesAsync();
    }
}
```

- [ ] **Step 3: Register the service**

In `ServiceConfigurationExtensions.cs`, directly under the `IInvoiceService` registration (`:95`), add:

```csharp
        services.AddScoped<IProjectDocumentService, ProjectDocumentService>();
```

- [ ] **Step 4: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Services/IProjectDocumentService.cs src/FellsideDigital.Web/Services/ProjectDocumentService.cs src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs
git commit -m "feat: add ProjectDocumentService"
```

---

### Task 4: Include `Documents` in project getters

**Files:**
- Modify: `src/FellsideDigital.Web/Services/ProjectService.cs:39-58`

- [ ] **Step 1: Add the include to both getters**

In `GetByIdAsync`, add a line after `.Include(p => p.PlanPhases.OrderBy(ph => ph.Order))`:

```csharp
            .Include(p => p.Documents.OrderByDescending(d => d.CreatedAt))
```

In `GetByIdForClientAsync`, add the identical line after its `.Include(p => p.PlanPhases.OrderBy(ph => ph.Order))`:

```csharp
            .Include(p => p.Documents.OrderByDescending(d => d.CreatedAt))
```

- [ ] **Step 2: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Services/ProjectService.cs
git commit -m "feat: load project documents in detail getters"
```

---

### Task 5: Admin Documents page

Mirrors the admin `Notes` page (back link, upload card, list with delete). Admin uploads a title + file; lists newest-first; delete removes the row + S3 object.

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor.cs`

- [ ] **Step 1: Create the code-behind**

```csharp
using System.Security.Claims;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Documents : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IProjectDocumentService DocumentService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private ClientProject? _project;
    private List<ProjectDocument> _documents = [];
    private Dictionary<Guid, string> _downloadUrls = [];

    private string _newTitle = "";
    private IBrowserFile? _selectedFile;
    private bool _saving;
    private string? _error;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        _documents = await DocumentService.GetForProjectAsync(Id);

        _downloadUrls = [];
        foreach (var doc in _documents)
        {
            try { _downloadUrls[doc.Id] = await DocumentService.GetDownloadUrlAsync(doc.Id) ?? ""; }
            catch { /* non-fatal */ }
        }
    }

    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        _selectedFile = e.File;
        if (string.IsNullOrWhiteSpace(_newTitle))
            _newTitle = Path.GetFileNameWithoutExtension(e.File.Name);
    }

    private async Task<string?> CurrentUserIdAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        return authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task UploadAsync()
    {
        _error = null;
        if (string.IsNullOrWhiteSpace(_newTitle) || _selectedFile is null) return;

        _saving = true;
        try
        {
            var actorId = await CurrentUserIdAsync();
            await DocumentService.UploadAsync(Id, _newTitle.Trim(), _selectedFile, actorId);
            _newTitle = "";
            _selectedFile = null;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _saving = false;
        }
        await LoadAsync();
    }

    private async Task DeleteAsync(Guid documentId)
    {
        await DocumentService.DeleteAsync(documentId);
        await LoadAsync();
    }
}
```

- [ ] **Step 2: Create the markup**

```razor
@page "/Admin/Projects/{Id:guid}/Documents"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout AdminLayout

@using FellsideDigital.Web.Services
@using Microsoft.AspNetCore.Authorization
@using FellsideDigital.Web.Components.Layout
@using FellsideDigital.Web.Components.Shared

<PageTitle>Project documents — Fellside Digital Admin</PageTitle>

@if (_project is null)
{
    <div class="animate-pulse space-y-4 max-w-3xl">
        <div class="h-4 w-24 rounded-md bg-gray-200 dark:bg-white/10"></div>
        <div class="rounded-2xl border border-gray-200 dark:border-white/5 bg-white dark:bg-neutral-900 h-56"></div>
    </div>
}
else
{
    <div class="max-w-3xl space-y-5">

        <!-- Header -->
        <div>
            <a href="@($"/Admin/Projects/{Id}")"
               class="inline-flex items-center gap-1 text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors mb-3">
                <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
                </svg>
                Back to project
            </a>
            <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">Project documents</h1>
            <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">@_project.Name · @_project.Client?.CompanyName</p>
        </div>

        <!-- Upload -->
        <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-white/5">
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Share a document</h2>
                <p class="mt-0.5 text-xs text-gray-500 dark:text-neutral-400">
                    Upload a proposal or any file you want the client to see. Everything here is visible to the client on their project page. PDF, Word or image, up to 25&nbsp;MB.
                </p>
            </div>
            <div class="p-5 space-y-4">
                @if (_error is not null)
                {
                    <p class="text-sm text-red-500">@_error</p>
                }
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-neutral-300 mb-1.5">Title <span class="text-red-500">*</span></label>
                    <input @bind="_newTitle" placeholder="e.g. Project proposal" class="@InputClass" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-neutral-300 mb-1.5">File <span class="text-red-500">*</span></label>
                    <InputFile OnChange="OnFileSelected" accept=".pdf,.png,.jpg,.jpeg,.doc,.docx"
                               class="block w-full text-sm text-gray-600 dark:text-neutral-300 file:mr-3 file:rounded-lg file:border-0 file:bg-accent file:px-3.5 file:py-2 file:text-sm file:font-semibold file:text-white hover:file:opacity-90" />
                    @if (_selectedFile is not null)
                    {
                        <p class="mt-1.5 text-xs text-gray-400 dark:text-neutral-500">@_selectedFile.Name</p>
                    }
                </div>
                <div class="flex justify-end">
                    <SpinnerButton IsLoading="@_saving"
                                   LoadingText="Uploading…"
                                   Disabled="@(string.IsNullOrWhiteSpace(_newTitle) || _selectedFile is null)"
                                   OnClick="UploadAsync">
                        <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5M16.5 7.5 12 3m0 0L7.5 7.5M12 3v13.5" />
                        </svg>
                        <span>Upload</span>
                    </SpinnerButton>
                </div>
            </div>
        </div>

        <!-- List -->
        <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 shadow-sm overflow-hidden">
            <div class="px-5 py-4 border-b border-gray-100 dark:border-white/5">
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Documents</h2>
            </div>
            @if (_documents.Count == 0)
            {
                <div class="px-5 py-8 text-center">
                    <p class="text-sm text-gray-400 dark:text-neutral-500">No documents yet.</p>
                </div>
            }
            else
            {
                <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                    @foreach (var doc in _documents)
                    {
                        <li class="flex items-center gap-4 px-5 py-4">
                            <div class="min-w-0 flex-1">
                                <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">@doc.Title</p>
                                <p class="mt-0.5 text-[11px] text-gray-400 dark:text-neutral-500 truncate">
                                    @doc.FileName · @doc.CreatedAt.ToLocalTime().ToString("d MMM yyyy, HH:mm")
                                </p>
                            </div>
                            <div class="flex items-center gap-2 shrink-0">
                                @if (_downloadUrls.TryGetValue(doc.Id, out var url) && !string.IsNullOrEmpty(url))
                                {
                                    <a href="@url" target="_blank" rel="noopener noreferrer"
                                       class="text-xs font-medium text-accent hover:opacity-80 transition-opacity">View ↗</a>
                                }
                                <button type="button" @onclick="() => DeleteAsync(doc.Id)"
                                        class="rounded-lg p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-500/10 transition-colors" title="Delete">
                                    <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="m14.74 9-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 0 1-2.244 2.077H8.084a2.25 2.25 0 0 1-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 0 0-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 0 1 3.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 0 0-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 0 0-7.5 0" />
                                    </svg>
                                </button>
                            </div>
                        </li>
                    }
                </ul>
            }
        </div>
    </div>
}
```

- [ ] **Step 3: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor src/FellsideDigital.Web/Components/Pages/Admin/Projects/Documents.razor.cs
git commit -m "feat: admin project documents page"
```

---

### Task 6: Link to Documents from the admin Detail page

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor:316-333` (the Timeline `CardSection` header)

- [ ] **Step 1: Add a "Manage documents" link**

In the Timeline `CardSection`'s `<Header>` (the `div` at `:319` holding the title and the "Manage notes →" link), change the link group so both links appear. Replace:

```razor
                            <a href="@($"/Admin/Projects/{Id}/Notes")"
                               class="text-sm font-semibold text-accent hover:opacity-80 transition-opacity">Manage notes →</a>
```

with:

```razor
                            <div class="flex items-center gap-3">
                                <a href="@($"/Admin/Projects/{Id}/Documents")"
                                   class="text-sm font-semibold text-accent hover:opacity-80 transition-opacity">Documents →</a>
                                <a href="@($"/Admin/Projects/{Id}/Notes")"
                                   class="text-sm font-semibold text-accent hover:opacity-80 transition-opacity">Manage notes →</a>
                            </div>
```

- [ ] **Step 2: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor
git commit -m "feat: link to documents page from admin project detail"
```

---

### Task 7: Load document download URLs in the portal page

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs`

- [ ] **Step 1: Inject the service + add a URL map**

After the `IInvoiceService InvoiceService` injection line, add:

```csharp
    [Inject] private IProjectDocumentService DocumentService { get; set; } = default!;
```

After the `private Dictionary<Guid, string> _downloadUrls = [];` field, add:

```csharp
    private Dictionary<Guid, string> _documentUrls = [];
```

- [ ] **Step 2: Build the document URLs**

In `OnInitializedAsync`, directly after the existing invoice `_downloadUrls` loop, add:

```csharp
        _documentUrls = [];
        foreach (var doc in _project.Documents)
        {
            try { _documentUrls[doc.Id] = await DocumentService.GetDownloadUrlAsync(doc.Id) ?? ""; }
            catch { /* non-fatal */ }
        }
```

- [ ] **Step 3: Pass the map to the view**

In `ProjectDetail.razor`, update the component usage to pass the new parameter (added in Task 8):

```razor
<PortalProjectDetailView Project="_project" DownloadUrls="_downloadUrls" DocumentUrls="_documentUrls" />
```

- [ ] **Step 4: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded **only after Task 8 adds the `DocumentUrls` parameter** — if building this task alone fails solely on the missing `DocumentUrls` parameter, that's expected; proceed to Task 8 then build. (Commit together with Task 8.)

---

### Task 8: Rewrite `PortalProjectDetailView.razor` (the new dashboard layout)

The centrepiece. Replaces the whole file with: header (name + status/type chips + visit button + description line) → inline stat bar (Progress / Current phase / Target launch / Outstanding) → full-width hero preview (website iframe, automation tool card, or placeholder) → lower grid of Project plan (phase rail) + Documents + Invoices. No timeline panel, no old `ProgressSteps`, no project-details dl.

**Files:**
- Modify (full replace): `src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor`

- [ ] **Step 1: Replace the entire file with:**

```razor
@using FellsideDigital.Domain.Enums
@using FellsideDigital.Domain.Extensions
@using FellsideDigital.Web.Data
@* BadgeHelpers is in FellsideDigital.Web.Components.Shared, already imported via _Imports.razor *@

@{
    var isWebsite    = Project.Type == ProjectType.Website;
    var isAutomation = Project.Type == ProjectType.Automation;
    var resolvedBackHref  = BackHref  ?? (isAutomation ? "/Portal/Automations" : "/Portal/Websites");
    var resolvedBackLabel = BackLabel ?? (isAutomation ? "My automations" : "My websites");
    var liveUrl   = Project.ProjectUrl;
    var phases    = Project.PlanPhases.OrderBy(p => p.Order).ToList();
    var current   = CurrentPhase(phases);
    var outstanding = Project.Invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
        .ToList();
    var outstandingTotal = outstanding.Sum(i => i.Amount);
    var nextDue = outstanding.Where(i => i.DueAt.HasValue).OrderBy(i => i.DueAt).FirstOrDefault();
    var currency = outstanding.FirstOrDefault()?.Currency ?? "£";
    var daysToLaunch = Project.TargetLaunchDate is { } t ? (t.Date - DateTime.UtcNow.Date).Days : (int?)null;
}

<div class="flex flex-col gap-6">

    <!-- Header -->
    <div>
        <NavLink href="@resolvedBackHref"
                 class="inline-flex items-center gap-1 text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors mb-3">
            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
            </svg>
            @resolvedBackLabel
        </NavLink>

        <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2.5">
                    <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">@Project.Name</h1>
                    <StatusBadge CssClasses="@BadgeHelpers.ProjectStatusBadge(Project.Status)" Label="@Project.Status.DisplayName()" />
                    <StatusBadge CssClasses="@(isAutomation ? "bg-accent-hover text-accent" : "bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-neutral-400")" Label="@Project.Type.DisplayName()" />
                </div>
                @if (!string.IsNullOrWhiteSpace(Project.Description))
                {
                    <p class="mt-1.5 max-w-2xl text-sm text-gray-500 dark:text-neutral-400 leading-relaxed">@Project.Description</p>
                }
            </div>
            @if (!string.IsNullOrEmpty(liveUrl))
            {
                <a href="@liveUrl" target="_blank" rel="noopener noreferrer"
                   class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5 bg-accent text-white text-sm font-semibold shadow-sm hover:opacity-90 active:scale-95 transition-all shrink-0">
                    <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                    </svg>
                    @(isAutomation ? "Open tool" : "Visit live site")
                </a>
            }
        </div>
    </div>

    <!-- Stat bar -->
    <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm
                grid grid-cols-2 lg:grid-cols-4 divide-x divide-y lg:divide-y-0 divide-gray-100 dark:divide-white/5">
        <div class="px-5 py-4">
            <p class="text-[11px] font-medium uppercase tracking-wider text-gray-400 dark:text-neutral-500">Progress</p>
            <p class="mt-1.5 text-2xl font-bold tracking-tight text-gray-900 dark:text-white">@Project.ProgressPercent%</p>
            <div class="mt-2 h-1.5 max-w-[150px] rounded-full bg-gray-100 dark:bg-white/10 overflow-hidden">
                <div class="h-full rounded-full bg-accent" style="width:@(Project.ProgressPercent)%"></div>
            </div>
        </div>
        <div class="px-5 py-4">
            <p class="text-[11px] font-medium uppercase tracking-wider text-gray-400 dark:text-neutral-500">Current phase</p>
            <p class="mt-1.5 text-base font-semibold text-gray-900 dark:text-white truncate">@(current?.Title ?? "—")</p>
            @if (current is not null)
            {
                <p class="mt-0.5 text-xs text-gray-400 dark:text-neutral-500">Phase @(phases.IndexOf(current) + 1) of @phases.Count</p>
            }
        </div>
        <div class="px-5 py-4">
            <p class="text-[11px] font-medium uppercase tracking-wider text-gray-400 dark:text-neutral-500">Target launch</p>
            <p class="mt-1.5 text-base font-semibold text-gray-900 dark:text-white">@(Project.TargetLaunchDate?.ToLocalTime().ToString("d MMM yyyy") ?? "—")</p>
            @if (daysToLaunch is { } d)
            {
                <p class="mt-0.5 text-xs text-gray-400 dark:text-neutral-500">@(d >= 0 ? $"In {d} days" : $"{-d} days ago")</p>
            }
        </div>
        <div class="px-5 py-4">
            <p class="text-[11px] font-medium uppercase tracking-wider text-gray-400 dark:text-neutral-500">Outstanding</p>
            @if (outstandingTotal > 0)
            {
                <p class="mt-1.5 text-base font-semibold text-gray-900 dark:text-white">@currency @outstandingTotal.ToString("N2")</p>
                @if (nextDue is not null)
                {
                    <p class="mt-0.5 text-xs text-gray-400 dark:text-neutral-500">@nextDue.Title · due @nextDue.DueAt!.Value.ToLocalTime().ToString("d MMM")</p>
                }
            }
            else
            {
                <p class="mt-1.5 text-base font-semibold text-gray-900 dark:text-white">All settled</p>
                <p class="mt-0.5 text-xs text-gray-400 dark:text-neutral-500">Nothing due</p>
            }
        </div>
    </div>

    <!-- Hero preview -->
    @if (isWebsite && !string.IsNullOrEmpty(Project.PreviewUrl))
    {
        <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 overflow-hidden bg-white dark:bg-neutral-900 shadow-sm">
            <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-white/5 bg-gray-50/80 dark:bg-white/[0.02]">
                <div class="flex items-center gap-1.5">
                    <div class="size-2.5 rounded-full bg-red-400/80"></div>
                    <div class="size-2.5 rounded-full bg-amber-400/80"></div>
                    <div class="size-2.5 rounded-full bg-emerald-400/80"></div>
                </div>
                <span class="text-xs text-gray-400 dark:text-neutral-500 truncate max-w-[180px] sm:max-w-xs font-mono">@Project.PreviewUrl</span>
                <a href="@Project.PreviewUrl" target="_blank" rel="noopener"
                   class="inline-flex items-center gap-1 text-xs font-medium text-accent hover:underline shrink-0">
                    Open
                    <svg class="size-3" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                    </svg>
                </a>
            </div>
            <iframe src="@Project.PreviewUrl" class="w-full h-[70vh]" title="@Project.Name website preview"
                    loading="lazy" sandbox="allow-scripts allow-same-origin allow-forms allow-popups"></iframe>
        </div>
    }
    else if (isAutomation && !string.IsNullOrEmpty(Project.ProjectUrl))
    {
        <div class="rounded-2xl border border-indigo-200 dark:border-white/5 bg-gradient-to-br from-accent-hover to-white dark:to-transparent p-6 flex items-center justify-between gap-4">
            <div class="flex items-center gap-4 min-w-0">
                <div class="flex size-12 shrink-0 items-center justify-center rounded-xl bg-indigo-100 text-accent">
                    <svg class="size-6" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z" />
                    </svg>
                </div>
                <div class="min-w-0">
                    <div class="flex items-center gap-2 mb-0.5">
                        <p class="text-sm font-bold text-indigo-900 dark:text-orange-300">Your automation tool</p>
                        <span class="flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
                            <span class="size-1.5 rounded-full bg-emerald-500 animate-pulse"></span> Live
                        </span>
                    </div>
                    <p class="text-xs text-accent/70 truncate">@Project.ProjectUrl</p>
                </div>
            </div>
            <a href="@Project.ProjectUrl" target="_blank" rel="noopener noreferrer"
               class="shrink-0 inline-flex items-center gap-1.5 rounded-xl px-4 py-2.5 bg-accent text-white text-sm font-semibold hover:opacity-90 transition-opacity shadow-sm">
                Open tool
                <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                </svg>
            </a>
        </div>
    }
    else
    {
        <div class="rounded-2xl border border-dashed border-gray-200 dark:border-white/10 bg-gray-50/50 dark:bg-white/[0.02] py-16 text-center">
            <p class="text-sm text-gray-400 dark:text-neutral-500">Preview coming soon</p>
        </div>
    }

    <!-- Lower grid -->
    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">

        <!-- Project plan -->
        <div class="lg:col-span-2 rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5 flex items-center justify-between">
                <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project plan</h2>
                @if (phases.Count > 0)
                {
                    <span class="text-xs text-gray-400 dark:text-neutral-500">@phases.Count phases</span>
                }
            </div>
            @if (phases.Count == 0)
            {
                <div class="px-6 py-10 text-center">
                    <p class="text-sm text-gray-400 dark:text-neutral-500">Plan coming soon.</p>
                </div>
            }
            else
            {
                <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                    @foreach (var phase in phases)
                    {
                        var isDone = phase.Status == PhaseStatus.Completed;
                        var isNow  = phase == current && !isDone;
                        var message = !string.IsNullOrWhiteSpace(phase.ImportantInformation) ? phase.ImportantInformation
                                    : !string.IsNullOrWhiteSpace(phase.Notes) ? phase.Notes : null;
                        var dateLine = isDone && phase.TargetCompletionDate is { } cd ? $"Completed {cd.ToLocalTime():d MMM yyyy}"
                                     : phase.TargetCompletionDate is { } td ? $"Target {td.ToLocalTime():d MMM yyyy}" : null;
                        <li class="flex gap-3.5 px-6 py-4">
                            <span class="mt-1.5 size-2.5 shrink-0 rounded-full @(isDone ? "bg-emerald-500" : isNow ? "bg-accent ring-4 ring-accent/15" : "bg-gray-300 dark:bg-white/15")"></span>
                            <div class="min-w-0">
                                <p class="text-sm font-semibold @(isNow ? "text-accent" : "text-gray-900 dark:text-white")">@phase.Title</p>
                                @if (dateLine is not null || message is not null)
                                {
                                    <p class="mt-0.5 text-xs text-gray-400 dark:text-neutral-500">
                                        @dateLine@(dateLine is not null && message is not null ? " · " : "")@message
                                    </p>
                                }
                            </div>
                        </li>
                    }
                </ul>
            }
        </div>

        <!-- Side: documents + invoices -->
        <div class="space-y-6">

            <!-- Documents -->
            <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5">
                    <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Documents</h2>
                </div>
                @if (!Project.Documents.Any())
                {
                    <div class="px-6 py-8 text-center">
                        <p class="text-sm text-gray-400 dark:text-neutral-500">No documents yet.</p>
                    </div>
                }
                else
                {
                    <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                        @foreach (var doc in Project.Documents.OrderByDescending(d => d.CreatedAt))
                        {
                            <li class="flex items-center gap-3 px-6 py-3.5">
                                <div class="min-w-0 flex-1">
                                    <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">@doc.Title</p>
                                    <p class="text-xs text-gray-400 dark:text-neutral-500">Shared @doc.CreatedAt.ToLocalTime().ToString("d MMM yyyy")</p>
                                </div>
                                @if (DocumentUrls.TryGetValue(doc.Id, out var durl) && !string.IsNullOrEmpty(durl))
                                {
                                    <a href="@durl" target="_blank" rel="noopener noreferrer"
                                       class="text-xs font-medium text-accent hover:opacity-80 transition-opacity shrink-0">View ↗</a>
                                }
                            </li>
                        }
                    </ul>
                }
            </div>

            <!-- Invoices -->
            <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5">
                    <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Invoices</h2>
                </div>
                @if (!Project.Invoices.Any())
                {
                    <div class="px-6 py-8 text-center">
                        <p class="text-sm text-gray-400 dark:text-neutral-500">No invoices yet.</p>
                    </div>
                }
                else
                {
                    <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                        @foreach (var inv in Project.Invoices.OrderByDescending(i => i.IssuedAt))
                        {
                            <li class="flex items-center gap-3 px-6 py-3.5">
                                <div class="min-w-0 flex-1">
                                    <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">@inv.Title</p>
                                    <p class="text-xs text-gray-400 dark:text-neutral-500">
                                        @inv.Currency @inv.Amount.ToString("N2")
                                        @if (inv.DueAt.HasValue && inv.Status != InvoiceStatus.Paid)
                                        {
                                            <span class="@(inv.DueAt < DateTime.UtcNow ? "text-red-500 dark:text-red-400 font-medium" : "")"> · due @inv.DueAt.Value.ToLocalTime().ToString("d MMM")</span>
                                        }
                                    </p>
                                </div>
                                <div class="flex items-center gap-2 shrink-0">
                                    <StatusBadge CssClasses="@BadgeHelpers.InvoiceStatusBadge(inv.Status)" Label="@inv.Status.ToString()" />
                                    @if (DownloadUrls.TryGetValue(inv.Id, out var url) && !string.IsNullOrEmpty(url))
                                    {
                                        <a href="@url" target="_blank" rel="noopener noreferrer"
                                           class="text-xs font-medium text-accent hover:opacity-80 transition-opacity">Download</a>
                                    }
                                </div>
                            </li>
                        }
                    </ul>
                }
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public ClientProject Project { get; set; } = default!;
    [Parameter, EditorRequired] public Dictionary<Guid, string> DownloadUrls { get; set; } = [];
    [Parameter] public Dictionary<Guid, string> DocumentUrls { get; set; } = [];
    [Parameter] public string? BackHref { get; set; }
    [Parameter] public string? BackLabel { get; set; }

    // Current phase = the in-progress one; else the first not-completed; else the last.
    private static ProjectPlanPhase? CurrentPhase(List<ProjectPlanPhase> phases)
    {
        if (phases.Count == 0) return null;
        return phases.FirstOrDefault(p => p.Status == PhaseStatus.InProgress)
            ?? phases.FirstOrDefault(p => p.Status != PhaseStatus.Completed)
            ?? phases[^1];
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeded (this resolves the `DocumentUrls` parameter referenced in Task 7).

- [ ] **Step 3: Commit (Tasks 7 + 8 together)**

```bash
git add src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor.cs
git commit -m "feat: redesign client project page as dashboard with documents"
```

---

### Task 9: Manual verification walkthrough

No automated tests exist; verify by running the app.

- [ ] **Step 1: Run** — `dotnet.exe run --project src/FellsideDigital.Web/FellsideDigital.Web.csproj --launch-profile http` (or rebuild in the VS Docker setup on :8080).
- [ ] **Step 2: Admin** — open a project's Detail page → click **Documents →** → upload a PDF titled "Project proposal" → confirm it lists with a working **View ↗** link → confirm the timeline shows a "Document shared" entry.
- [ ] **Step 3: Website client view** — open `/Portal/Projects/{Id}` for a Website project: confirm stat bar (Progress/Current phase/Target launch/Outstanding), large preview iframe, phase rail, the uploaded document, and invoices all render; no timeline panel; description shows under the title.
- [ ] **Step 4: Automation client view** — open an Automation project: confirm the "Open your tool" card fills the hero slot instead of an iframe; everything else renders.
- [ ] **Step 5: Empty states** — a project with no phases shows "Plan coming soon."; no documents shows "No documents yet."; no `TargetLaunchDate` shows "—".
- [ ] **Step 6: Delete** — delete the document on the admin page → confirm it disappears from both admin and the client page.

---

## Self-Review Notes

- **Spec coverage:** stat bar (Progress/Current phase/Target launch/Outstanding) ✓ Task 8; hero preview website/automation/placeholder ✓ Task 8; phase rail with client-facing message (`ImportantInformation`→`Notes`, never `InternalNotes`/`Dependencies`) ✓ Task 8; `ProjectDocument` + service reusing S3/presigned-URL pattern ✓ Tasks 1-3; `DocumentShared` timeline event ✓ Task 3; admin Documents page mirroring Notes ✓ Task 5; getters include Documents ✓ Task 4; portal loads `_documentUrls` ✓ Task 7; removed old `ProgressSteps`/details dl/timeline panel + `DeploymentNotes` hidden + `Description` moved to header ✓ Task 8.
- **Type consistency:** `IProjectDocumentService` signatures (`UploadAsync`/`GetForProjectAsync`/`GetDownloadUrlAsync`/`DeleteAsync`) are identical across interface (Task 3), admin page (Task 5), and portal code-behind (Task 7). `DocumentUrls` parameter name matches between Task 7's call site and Task 8's `[Parameter]`. `CurrentPhase` defined and used only in Task 8.
- **Cross-task build note:** Task 7 alone won't compile until Task 8 adds `DocumentUrls`; they share one commit — called out explicitly in both tasks.
```
