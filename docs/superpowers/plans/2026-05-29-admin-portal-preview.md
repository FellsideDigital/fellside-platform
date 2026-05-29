# Admin Portal Preview — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow admins to view any client's portal project detail and projects list in a safe, read-only preview that renders identically to what the client sees.

**Architecture:** Two new pages under `/Admin/Projects/{Id}/` protected by `SiteAdmin` role and rendered with `PortalLayout`. The portal project detail markup is extracted into a shared display-only component (`PortalProjectDetailView.razor`) reused by both the real portal page and the admin preview page. The existing portal pages are not modified except to use the shared component.

**Tech Stack:** Blazor Server (.NET 10), ASP.NET Identity, Tailwind CSS, C# partial classes (code-behind pattern)

---

## File Map

| File | Action |
|---|---|
| `src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor` | **Create** — display-only component, parameters only, no event handlers |
| `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor` | **Modify** — replace body markup with `<PortalProjectDetailView>` passthrough; keep skeleton/notfound states |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor` | **Create** — admin preview page for project detail |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor.cs` | **Create** — loads project by ID (no ownership check), passes to shared component |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor` | **Create** — admin preview page for client's projects list |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor.cs` | **Create** — loads project → gets ClientId → loads all client projects |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor` | **Modify** — add "Preview as client" button to header |
| `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Index.razor` | **Modify** — add "Portal view" link in actions column |

---

### Task 1: Create PortalProjectDetailView shared component

This extracts the view-only markup from `Portal/ProjectDetail.razor` into a reusable display component. It accepts the project and download URL map as parameters. The `BackHref`/`BackLabel` parameters allow callers to override the back navigation link (admin preview uses a different back destination than the real portal).

**Files:**
- Create: `src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor`

- [ ] **Create the shared component**

Create `src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor` with this exact content:

```razor
@using FellsideDigital.Domain.Enums
@using FellsideDigital.Web.Data
@using FellsideDigital.UI.Helpers

@{
    var isWebsite    = Project.Type == ProjectType.Website;
    var isAutomation = Project.Type == ProjectType.Automation;
    var resolvedBackHref  = BackHref  ?? (isAutomation ? "/Portal/Automations" : "/Portal/Websites");
    var resolvedBackLabel = BackLabel ?? (isAutomation ? "My automations" : "My websites");
}

<div class="flex flex-col gap-6">

    <!-- Back + header -->
    <div>
        <NavLink href="@resolvedBackHref"
                 class="inline-flex items-center gap-1 text-xs font-medium text-gray-500 dark:text-neutral-400
                        hover:text-gray-900 dark:hover:text-white transition-colors mb-3">
            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M15.75 19.5 8.25 12l7.5-7.5" />
            </svg>
            @resolvedBackLabel
        </NavLink>

        <div class="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
            <div>
                <div class="flex flex-wrap items-center gap-2.5">
                    <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">@Project.Name</h1>
                    <StatusBadge CssClasses="@BadgeHelpers.ProjectStatusBadge(Project.Status)"
                                 Label="@Project.Status.DisplayName()" />
                    <StatusBadge CssClasses="@(isAutomation ? "bg-accent-hover text-accent" : "bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-neutral-400")"
                                 Label="@Project.Type.DisplayName()" />
                </div>
                <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">
                    Started @Project.CreatedAt.ToLocalTime().ToString("d MMMM yyyy")
                </p>
            </div>
            @if (!string.IsNullOrEmpty(Project.ProjectUrl))
            {
                <a href="@Project.ProjectUrl" target="_blank" rel="noopener noreferrer"
                   class="inline-flex items-center gap-2 rounded-xl px-4 py-2.5
                          bg-accent-hover0 text-white text-sm font-semibold
                          shadow-sm hover:opacity-90 active:scale-95 transition-all shrink-0">
                    <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                    </svg>
                    @(isAutomation ? "Open tool" : "Visit live site")
                </a>
            }
        </div>
    </div>

    <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">

        <!-- Left: preview / tool card + invoices -->
        <div class="lg:col-span-2 space-y-5">

            @if (isWebsite && !string.IsNullOrEmpty(Project.PreviewUrl))
            {
                <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 overflow-hidden bg-white dark:bg-neutral-900 shadow-sm">
                    <div class="flex items-center justify-between px-4 py-3
                                border-b border-gray-100 dark:border-white/5
                                bg-gray-50/80 dark:bg-white/[0.02]">
                        <div class="flex items-center gap-1.5">
                            <div class="size-2.5 rounded-full bg-red-400/80"></div>
                            <div class="size-2.5 rounded-full bg-amber-400/80"></div>
                            <div class="size-2.5 rounded-full bg-emerald-400/80"></div>
                        </div>
                        <span class="text-xs text-gray-400 dark:text-neutral-500 truncate max-w-[180px] sm:max-w-xs font-mono">@Project.PreviewUrl</span>
                        <a href="@Project.PreviewUrl" target="_blank" rel="noopener"
                           class="inline-flex items-center gap-1 text-xs font-medium text-accent-hover0 hover:underline shrink-0">
                            Open
                            <svg class="size-3" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                            </svg>
                        </a>
                    </div>
                    <iframe src="@Project.PreviewUrl"
                            class="w-full h-[65vh]"
                            title="@Project.Name website preview"
                            loading="lazy"
                            sandbox="allow-scripts allow-same-origin allow-forms allow-popups">
                    </iframe>
                </div>
            }

            @if (isAutomation && !string.IsNullOrEmpty(Project.ProjectUrl))
            {
                <div class="rounded-2xl border border-indigo-200
                            bg-gradient-to-br from-accent-hover to-white dark:to-transparent
                            p-5 flex items-center justify-between gap-4">
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
                                    <span class="size-1.5 rounded-full bg-emerald-500 animate-pulse"></span>
                                    Live
                                </span>
                            </div>
                            <p class="text-xs text-accent/70 truncate">@Project.ProjectUrl</p>
                        </div>
                    </div>
                    <a href="@Project.ProjectUrl" target="_blank" rel="noopener noreferrer"
                       class="shrink-0 inline-flex items-center gap-1.5 rounded-xl px-4 py-2.5
                              bg-accent-hover0 text-white text-sm font-semibold
                              hover:opacity-90 transition-opacity shadow-sm">
                        Open tool
                        <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 6H5.25A2.25 2.25 0 0 0 3 8.25v10.5A2.25 2.25 0 0 0 5.25 21h10.5A2.25 2.25 0 0 0 18 18.75V10.5m-10.5 6L21 3m0 0h-5.25M21 3v5.25" />
                        </svg>
                    </a>
                </div>
            }

            @if (Project.Invoices.Any())
            {
                <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
                    <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5">
                        <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Invoices</h2>
                    </div>
                    <ul role="list" class="divide-y divide-gray-100 dark:divide-white/5">
                        @foreach (var inv in Project.Invoices.OrderByDescending(i => i.IssuedAt))
                        {
                            <li class="flex items-center gap-4 px-6 py-4">
                                <div class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-gray-100 dark:bg-white/5">
                                    <svg class="size-4 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z" />
                                    </svg>
                                </div>
                                <div class="min-w-0 flex-1">
                                    <p class="text-sm font-semibold text-gray-900 dark:text-white truncate">@inv.Title</p>
                                    <p class="text-xs text-gray-400 dark:text-neutral-500 mt-0.5">
                                        @inv.IssuedAt.ToLocalTime().ToString("d MMM yyyy")
                                        @if (inv.DueAt.HasValue && inv.Status != InvoiceStatus.Paid)
                                        {
                                            <span class="@(inv.DueAt < DateTime.UtcNow ? "text-red-500 dark:text-red-400 font-medium" : "")">
                                                · Due @inv.DueAt.Value.ToLocalTime().ToString("d MMM yyyy")
                                            </span>
                                        }
                                    </p>
                                </div>
                                <div class="flex items-center gap-3 shrink-0">
                                    <span class="text-sm font-bold text-gray-900 dark:text-white">@inv.Currency @inv.Amount.ToString("N2")</span>
                                    <StatusBadge CssClasses="@BadgeHelpers.InvoiceStatusBadge(inv.Status)" Label="@inv.Status.ToString()" />
                                    @if (DownloadUrls.TryGetValue(inv.Id, out var url) && !string.IsNullOrEmpty(url))
                                    {
                                        <a href="@url" target="_blank" rel="noopener noreferrer"
                                           class="inline-flex items-center gap-1 text-xs font-medium
                                                  text-accent-hover0 hover:text-accent dark:hover:text-orange-300 transition-colors">
                                            <svg class="size-3.5" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                                <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 0 0 5.25 21h13.5A2.25 2.25 0 0 0 21 18.75V16.5M16.5 12 12 16.5m0 0L7.5 12m4.5 4.5V3" />
                                            </svg>
                                            Download
                                        </a>
                                    }
                                </div>
                            </li>
                        }
                    </ul>
                </div>
            }
        </div>

        <!-- Right: details + progress + updates -->
        <div class="space-y-5">

            <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5">
                    <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project details</h2>
                </div>
                <dl class="divide-y divide-gray-100 dark:divide-white/5">
                    <div class="px-6 py-4">
                        <dt class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-1.5">Status</dt>
                        <dd>
                            <span class="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold @BadgeHelpers.ProjectStatusBadge(Project.Status)">
                                <span class="size-1.5 rounded-full @BadgeHelpers.ProjectStatusDotColor(Project.Status)"></span>
                                @Project.Status.DisplayName()
                            </span>
                        </dd>
                    </div>
                    <div class="grid grid-cols-2">
                        <div class="px-6 py-4">
                            <dt class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-1">Type</dt>
                            <dd class="text-sm font-medium text-gray-900 dark:text-white">@Project.Type.DisplayName()</dd>
                        </div>
                        <div class="px-6 py-4 border-l border-gray-100 dark:border-white/5">
                            <dt class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-1">Started</dt>
                            <dd class="text-sm font-medium text-gray-900 dark:text-white">@Project.CreatedAt.ToLocalTime().ToString("d MMM yyyy")</dd>
                        </div>
                    </div>
                    <div class="px-6 py-4">
                        <dt class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-2">About this project</dt>
                        <dd class="text-sm text-gray-600 dark:text-neutral-300 leading-relaxed">@Project.Description</dd>
                    </div>
                    @if (!string.IsNullOrEmpty(Project.DeploymentNotes))
                    {
                        <div class="px-6 py-4">
                            <dt class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-2">
                                @(isWebsite ? "Deployment notes" : "Technical notes")
                            </dt>
                            <dd class="text-sm text-gray-600 dark:text-neutral-300 leading-relaxed">@Project.DeploymentNotes</dd>
                        </div>
                    }
                </dl>
                <div class="px-6 py-5 border-t border-gray-100 dark:border-white/5 bg-gray-50/50 dark:bg-white/[0.02]">
                    <p class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider mb-4">Progress</p>
                    <ProgressSteps Steps='@(new[] { "Scoping", "Building", "Complete" })'
                                   CurrentStep="@StatusToStep(Project.Status)"
                                   Variant="@StatusToVariant(Project.Status)"
                                   OverrideLabel="@StatusToLabel(Project.Status)" />
                </div>
            </div>

            <div class="rounded-2xl bg-white dark:bg-neutral-900 border border-gray-200/80 dark:border-white/5 shadow-sm overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-100 dark:border-white/5">
                    <h2 class="text-sm font-semibold text-gray-900 dark:text-white">Project updates</h2>
                </div>
                @if (!Project.StatusUpdates.Any())
                {
                    <div class="px-6 py-10 text-center">
                        <div class="mx-auto mb-3 flex size-10 items-center justify-center rounded-xl bg-gray-100 dark:bg-white/5">
                            <svg class="size-5 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" d="M7.5 8.25h9m-9 3H12m-9.75 1.51c0 1.6 1.123 2.994 2.707 3.227 1.129.166 2.27.293 3.423.379.35.026.67.21.865.501L12 21l2.755-4.133a1.14 1.14 0 0 1 .865-.501 48.172 48.172 0 0 0 3.423-.379c1.584-.233 2.707-1.626 2.707-3.228V6.741c0-1.602-1.123-2.995-2.707-3.228A48.394 48.394 0 0 0 12 3c-2.392 0-4.744.175-7.043.513C3.373 3.746 2.25 5.14 2.25 6.741v6.018Z" />
                            </svg>
                        </div>
                        <p class="text-sm text-gray-400 dark:text-neutral-500">No updates yet.</p>
                    </div>
                }
                else
                {
                    <ul role="list" class="px-6 py-5 space-y-6">
                        @{
                            var updates = Project.StatusUpdates.OrderByDescending(u => u.CreatedAt).ToList();
                        }
                        @for (int i = 0; i < updates.Count; i++)
                        {
                            var update = updates[i];
                            var isLast = i == updates.Count - 1;
                            <li class="relative flex gap-x-4">
                                @if (!isLast)
                                {
                                    <div class="absolute top-7 left-[13px] bottom-0 w-px bg-gray-200 dark:bg-white/10"></div>
                                }
                                <div class="relative flex size-7 flex-none items-center justify-center mt-0.5">
                                    @if (update.NewStatus.HasValue)
                                    {
                                        <div class="size-7 rounded-full bg-accent-hover flex items-center justify-center ring-2 ring-indigo-100">
                                            <div class="size-2.5 rounded-full bg-accent"></div>
                                        </div>
                                    }
                                    else
                                    {
                                        <div class="size-7 rounded-full bg-gray-100 dark:bg-white/5 flex items-center justify-center">
                                            <div class="size-2 rounded-full bg-gray-300 dark:bg-white/20"></div>
                                        </div>
                                    }
                                </div>
                                <div class="flex-auto min-w-0 @(isLast ? "" : "pb-2")">
                                    @if (update.NewStatus.HasValue)
                                    {
                                        <p class="text-xs text-gray-400 dark:text-neutral-500 mb-1">
                                            Status changed →
                                            <span class="font-semibold text-gray-900 dark:text-white">@update.NewStatus.Value.DisplayName()</span>
                                        </p>
                                    }
                                    <div class="rounded-xl bg-gray-50 dark:bg-white/5 border border-gray-100 dark:border-white/5 px-4 py-3">
                                        <p class="text-sm text-gray-700 dark:text-neutral-200 leading-relaxed">@update.Message</p>
                                        <time class="block mt-2 text-xs text-gray-400 dark:text-neutral-500">
                                            @update.CreatedAt.ToLocalTime().ToString("d MMMM yyyy 'at' HH:mm")
                                        </time>
                                    </div>
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
    [Parameter] public string? BackHref { get; set; }
    [Parameter] public string? BackLabel { get; set; }

    private static int StatusToStep(ProjectStatus s) => s switch
    {
        ProjectStatus.Pending    => 0,
        ProjectStatus.InProgress => 1,
        ProjectStatus.Blocked    => 1,
        ProjectStatus.OnHold     => 1,
        ProjectStatus.Completed  => 3,
        _                        => 0,
    };

    private static string StatusToVariant(ProjectStatus s) => s switch
    {
        ProjectStatus.Completed => "success",
        ProjectStatus.Blocked   => "blocked",
        ProjectStatus.OnHold    => "paused",
        _                       => "default",
    };

    private static string? StatusToLabel(ProjectStatus s) => s switch
    {
        ProjectStatus.Blocked => "Blocked",
        ProjectStatus.OnHold  => "On Hold",
        _                     => null,
    };
}
```

- [ ] **Build to verify compilation**

```bash
cd /mnt/d/Projects/fellside-digital-site/src/FellsideDigital.Web && dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Commit**

```bash
git add src/FellsideDigital.Web/Components/Shared/PortalProjectDetailView.razor
git commit -m "feat: add PortalProjectDetailView shared display component"
```

---

### Task 2: Refactor Portal/ProjectDetail.razor to use the shared component

Replace the full project detail body markup with a single `<PortalProjectDetailView>` call. Keep the skeleton and not-found states — only the `else if (_project is not null)` block changes. The `@code` block at the bottom of the file is removed entirely (the helper methods have moved into the shared component).

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor`

- [ ] **Replace the project detail body**

Open `src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor`.

Replace everything from line 51 (`else if (_project is not null)`) to the end of the file with:

```razor
else if (_project is not null)
{
    <PortalProjectDetailView Project="_project" DownloadUrls="_downloadUrls" />
}
```

The file should now end after this closing brace. The `@code { ... }` block that was at the bottom (containing `StatusToStep`, `StatusToVariant`, `StatusToLabel`) is gone — those methods now live in `PortalProjectDetailView.razor`.

- [ ] **Build to verify**

```bash
cd /mnt/d/Projects/fellside-digital-site/src/FellsideDigital.Web && dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Portal/ProjectDetail.razor
git commit -m "refactor: Portal/ProjectDetail uses shared PortalProjectDetailView component"
```

---

### Task 3: Create admin preview — project detail page

A new page at `/Admin/Projects/{Id:guid}/PortalPreview`. It requires `SiteAdmin` role, uses `PortalLayout`, and loads the project by ID with no ownership check. It passes the project data directly to `PortalProjectDetailView`. The `BackHref` is set to the client's projects list preview so the back link stays within admin preview routes.

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor.cs`

- [ ] **Create the Razor page**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor`:

```razor
@page "/Admin/Projects/{Id:guid}/PortalPreview"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout PortalLayout

@using Microsoft.AspNetCore.Authorization
@using FellsideDigital.Web.Components.Layout

<PageTitle>@(_project?.Name ?? "Preview") — Portal Preview</PageTitle>

@if (_project is null)
{
    <div class="animate-pulse space-y-6">
        <div class="h-4 w-24 rounded bg-gray-200 dark:bg-white/10"></div>
        <div class="h-8 w-72 rounded-lg bg-gray-200 dark:bg-white/10"></div>
        <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">
            <div class="lg:col-span-2 space-y-5">
                <div class="rounded-2xl bg-gray-100 dark:bg-white/5 h-56"></div>
                <div class="rounded-2xl bg-gray-100 dark:bg-white/5 h-40"></div>
            </div>
            <div class="space-y-5">
                <div class="rounded-2xl bg-gray-100 dark:bg-white/5 h-60"></div>
                <div class="rounded-2xl bg-gray-100 dark:bg-white/5 h-48"></div>
            </div>
        </div>
    </div>
}
else
{
    <PortalProjectDetailView Project="_project"
                             DownloadUrls="_downloadUrls"
                             BackHref="@($"/Admin/Projects/{Id}/PortalProjectsList")"
                             BackLabel="Client's projects" />
}
```

- [ ] **Create the code-behind**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor.cs`:

```csharp
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PreviewPortalDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private Dictionary<Guid, string> _downloadUrls = [];

    protected override async Task OnInitializedAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is null)
        {
            NavigationManager.NavigateTo("/Admin/Projects");
            return;
        }

        foreach (var inv in _project.Invoices.Where(i => i.FilePath is not null))
        {
            try { _downloadUrls[inv.Id] = await InvoiceService.GetDownloadUrlAsync(inv.Id) ?? ""; }
            catch { }
        }
    }
}
```

- [ ] **Build to verify**

```bash
cd /mnt/d/Projects/fellside-digital-site/src/FellsideDigital.Web && dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalDetail.razor.cs
git commit -m "feat: add admin portal preview — project detail page"
```

---

### Task 4: Create admin preview — client projects list page

A new page at `/Admin/Projects/{Id:guid}/PortalProjectsList`. It looks up the project to get the `ClientId`, loads all projects for that client, and renders the same list UI the client sees on `/Portal/Projects`. Each project row links to the project detail preview (`/Admin/Projects/{projectId}/PortalPreview`) rather than the real portal route, so navigation stays within admin preview.

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor.cs`

- [ ] **Create the Razor page**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor`:

```razor
@page "/Admin/Projects/{Id:guid}/PortalProjectsList"
@attribute [Authorize(Roles = "SiteAdmin")]
@layout PortalLayout

@using Microsoft.AspNetCore.Authorization
@using FellsideDigital.Domain.Enums
@using FellsideDigital.Web.Data
@using FellsideDigital.Web.Components.Layout
@using FellsideDigital.UI.Helpers

<PageTitle>Projects (Portal Preview) — Fellside Digital Admin</PageTitle>

<div class="mb-8">
    <p class="text-xs font-semibold text-accent-hover0 uppercase tracking-widest mb-1">Projects</p>
    <h1 class="text-2xl font-bold tracking-tight text-gray-900 dark:text-white">My projects</h1>
    <p class="mt-1 text-sm text-gray-500 dark:text-neutral-400">Track the status and progress of everything we're building together.</p>
</div>

@if (_projects is null)
{
    <div class="space-y-3 animate-pulse">
        @for (int i = 0; i < 3; i++)
        {
            <div class="rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 p-5">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-3">
                        <div class="size-9 rounded-xl bg-gray-100 dark:bg-white/5"></div>
                        <div class="space-y-2">
                            <div class="h-4 w-40 rounded-md bg-gray-200 dark:bg-white/10"></div>
                            <div class="h-3 w-24 rounded-md bg-gray-100 dark:bg-white/5"></div>
                        </div>
                    </div>
                    <div class="h-5 w-20 rounded-full bg-gray-100 dark:bg-white/5"></div>
                </div>
            </div>
        }
    </div>
}
else if (!_projects.Any())
{
    <EmptyState Padding="p-14"
                IconContainerClass="size-12 rounded-2xl mb-4"
                Title="No projects yet"
                Subtitle="Your project manager will set this up shortly.">
        <Icon>
            <svg class="size-6 text-gray-400 dark:text-neutral-500" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-8.69-6.44-2.12-2.12a1.5 1.5 0 0 0-1.061-.44H4.5A2.25 2.25 0 0 0 2.25 6v12a2.25 2.25 0 0 0 2.25 2.25h15A2.25 2.25 0 0 0 21.75 18V9a2.25 2.25 0 0 0-2.25-2.25h-5.379a1.5 1.5 0 0 1-1.06-.44Z" />
            </svg>
        </Icon>
    </EmptyState>
}
else
{
    <div class="space-y-3">
        @foreach (var project in _projects)
        {
            <NavLink href="@($"/Admin/Projects/{project.Id}/PortalPreview")"
                     class="group flex items-center justify-between rounded-2xl border border-gray-200/80 dark:border-white/5 bg-white dark:bg-neutral-900 p-5 hover:border-indigo-200 hover:shadow-sm transition-all">
                <div class="flex items-start gap-4 min-w-0 flex-1">
                    <div class="flex size-10 shrink-0 items-center justify-center rounded-xl bg-gray-100 dark:bg-white/5 group-hover:bg-accent-hover transition-colors mt-0.5">
                        <svg class="size-5 text-gray-400 dark:text-neutral-500 group-hover:text-accent-hover0 transition-colors" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M2.25 12.75V12A2.25 2.25 0 0 1 4.5 9.75h15A2.25 2.25 0 0 1 21.75 12v.75m-8.69-6.44-2.12-2.12a1.5 1.5 0 0 0-1.061-.44H4.5A2.25 2.25 0 0 0 2.25 6v12a2.25 2.25 0 0 0 2.25 2.25h15A2.25 2.25 0 0 0 21.75 18V9a2.25 2.25 0 0 0-2.25-2.25h-5.379a1.5 1.5 0 0 1-1.06-.44Z" />
                        </svg>
                    </div>
                    <div class="min-w-0 flex-1">
                        <div class="flex flex-wrap items-center gap-2 mb-1">
                            <p class="text-sm font-semibold text-gray-900 dark:text-white group-hover:text-accent transition-colors truncate">
                                @project.Name
                            </p>
                            <span class="hidden sm:inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 dark:bg-white/5 dark:text-neutral-400">
                                @project.Type
                            </span>
                        </div>
                        <p class="text-sm text-gray-500 dark:text-neutral-400 line-clamp-1">@project.Description</p>
                        <div class="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-400 dark:text-neutral-500">
                            <span>Started @project.CreatedAt.ToLocalTime().ToString("d MMM yyyy")</span>
                            <span>@project.Invoices.Count invoice@(project.Invoices.Count == 1 ? "" : "s")</span>
                            @if (project.StatusUpdates.Any())
                            {
                                <span>Last update @project.StatusUpdates.OrderByDescending(u => u.CreatedAt).First().CreatedAt.ToLocalTime().ToString("d MMM yyyy")</span>
                            }
                        </div>
                    </div>
                </div>
                <div class="flex items-center gap-3 shrink-0 ml-4">
                    <span class="hidden sm:block">
                        <StatusBadge CssClasses="@BadgeHelpers.ProjectStatusBadge(project.Status)"
                                     Label="@project.Status.DisplayName()" />
                    </span>
                    <svg class="size-4 text-gray-300 dark:text-neutral-600 group-hover:text-accent transition-colors" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
                    </svg>
                </div>
            </NavLink>
        }
    </div>
}
```

- [ ] **Create the code-behind**

Create `src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor.cs`:

```csharp
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PreviewPortalProjects : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<ClientProject>? _projects;

    protected override async Task OnInitializedAsync()
    {
        var project = await ProjectService.GetByIdAsync(Id);
        if (project is null)
        {
            NavigationManager.NavigateTo("/Admin/Projects");
            return;
        }
        _projects = await ProjectService.GetForClientAsync(project.ClientId);
    }
}
```

- [ ] **Build to verify**

```bash
cd /mnt/d/Projects/fellside-digital-site/src/FellsideDigital.Web && dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/PreviewPortalProjects.razor.cs
git commit -m "feat: add admin portal preview — client projects list page"
```

---

### Task 5: Add entry points in admin pages

Add "Preview as client" to the admin project detail header (alongside the existing "Edit project" button) and "Portal view" to the admin projects table (alongside "Manage" and "Edit").

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor`
- Modify: `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Index.razor`

- [ ] **Add "Preview as client" button to admin project detail**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor`, find this block (around line 54):

```razor
            <PrimaryButton Color="ButtonColor.Muted" Style="ButtonStyle.Outline" Href="@($"/Admin/Projects/{Id}/Edit")">
                <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125" />
                </svg>
                Edit project
            </PrimaryButton>
```

Replace it with:

```razor
            <PrimaryButton Color="ButtonColor.Muted" Style="ButtonStyle.Outline" Href="@($"/Admin/Projects/{Id}/PortalProjectsList")">
                <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 0 1 0-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178Z" />
                    <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 1 1-6 0 3 3 0 0 1 6 0Z" />
                </svg>
                Preview as client
            </PrimaryButton>
            <PrimaryButton Color="ButtonColor.Muted" Style="ButtonStyle.Outline" Href="@($"/Admin/Projects/{Id}/Edit")">
                <svg class="size-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" d="m16.862 4.487 1.687-1.688a1.875 1.875 0 1 1 2.652 2.652L10.582 16.07a4.5 4.5 0 0 1-1.897 1.13L6 18l.8-2.685a4.5 4.5 0 0 1 1.13-1.897l8.932-8.931Zm0 0L19.5 7.125" />
                </svg>
                Edit project
            </PrimaryButton>
```

- [ ] **Add "Portal view" link to admin projects table**

In `src/FellsideDigital.Web/Components/Pages/Admin/Projects/Index.razor`, find this block in the actions `<td>` (around line 128):

```razor
                        <td class="px-6 py-4 text-right">
                            <div class="flex items-center justify-end gap-3">
                                <a href="@($"/Admin/Projects/{project.Id}")"
                                   class="text-xs font-semibold text-accent-hover0 hover:text-accent dark:hover:text-orange-300 transition-colors">
                                    Manage
                                </a>
                                <span class="text-gray-200 dark:text-white/10">|</span>
                                <a href="@($"/Admin/Projects/{project.Id}/Edit")"
                                   class="text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">
                                    Edit
                                </a>
                            </div>
                        </td>
```

Replace it with:

```razor
                        <td class="px-6 py-4 text-right">
                            <div class="flex items-center justify-end gap-3">
                                <a href="@($"/Admin/Projects/{project.Id}")"
                                   class="text-xs font-semibold text-accent-hover0 hover:text-accent dark:hover:text-orange-300 transition-colors">
                                    Manage
                                </a>
                                <span class="text-gray-200 dark:text-white/10">|</span>
                                <a href="@($"/Admin/Projects/{project.Id}/Edit")"
                                   class="text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">
                                    Edit
                                </a>
                                <span class="text-gray-200 dark:text-white/10">|</span>
                                <a href="@($"/Admin/Projects/{project.Id}/PortalProjectsList")"
                                   class="text-xs font-medium text-gray-500 dark:text-neutral-400 hover:text-gray-900 dark:hover:text-white transition-colors">
                                    Portal view
                                </a>
                            </div>
                        </td>
```

- [ ] **Build to verify**

```bash
cd /mnt/d/Projects/fellside-digital-site/src/FellsideDigital.Web && dotnet build
```

Expected: `Build succeeded.` with 0 errors.

- [ ] **Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Admin/Projects/Detail.razor \
        src/FellsideDigital.Web/Components/Pages/Admin/Projects/Index.razor
git commit -m "feat: add portal preview entry points to admin project pages"
```
