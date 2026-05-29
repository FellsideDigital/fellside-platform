# Admin Portal Preview — Design Spec

**Date:** 2026-05-29  
**Status:** Approved

## Problem

Admins need to verify what a client's portal looks like before telling them to log in — e.g. checking the project progress view, confirming invoices display correctly, or reviewing a status update. Currently the only way to do this is to log in as that client, which requires sharing credentials and risks accidental data changes.

## Goal

Give admins a safe, read-only way to see what a specific client sees when they view their project in the portal.

---

## Architecture

Two new Blazor pages under `/Admin/`, both:
- Protected by `[Authorize(Roles = "SiteAdmin")]`
- Using `PortalLayout` (renders identical chrome to the real portal)
- Read-only by design — no mutation methods, no forms

The existing portal pages (`/Portal/Projects`, `/Portal/Projects/{Id}`) are **not modified**.

### New pages

| Route | Purpose |
|---|---|
| `/Admin/Projects/{Id}/PortalPreview` | Preview what the client sees on the project detail page |
| `/Admin/Projects/{Id}/PortalProjectsList` | Preview what the client sees on their Projects list |

Both routes take the admin project `Id` (GUID). The `PortalProjectsList` page looks up the project's `ClientId` and loads all projects for that client.

### Entry points added

- **Admin projects table** (`/Admin/Projects`): a "Portal view" link in the Actions column alongside "Manage" and "Edit"
- **Admin project detail** (`/Admin/Projects/{Id}`): a "Preview as client" button in the page header alongside "Edit project"

---

## Components

### Shared view component: `PortalProjectDetailView.razor`

The portal's `ProjectDetail.razor` has ~370 lines of Razor markup. Rather than copy it, extract the view into a shared component in `Components/Shared/`:

```
Components/Shared/PortalProjectDetailView.razor
```

**Parameters:**
- `ClientProject Project` — the project to render
- `Dictionary<Guid, string> DownloadUrls` — presigned S3 URLs for invoice files

The existing `Portal/ProjectDetail.razor` becomes a thin wrapper that passes data to this component. The new admin preview page also uses it.

This component is **render-only** — no `@onchange`, no `@onclick`, no `EventCallback` parameters.

### New page: `PreviewPortalDetail.razor` + `.cs`

- Route: `/Admin/Projects/{Id:guid}/PortalPreview`
- Layout: `PortalLayout`
- Auth: `SiteAdmin`
- Code-behind: loads project by ID via `IProjectService.GetByIdAsync` (no ownership check), loads presigned download URLs for invoices, passes data to `PortalProjectDetailView`
- If project not found: redirect to `/Admin/Projects`

### New page: `PreviewPortalProjects.razor` + `.cs`

- Route: `/Admin/Projects/{Id:guid}/PortalProjectsList`
- Layout: `PortalLayout`
- Auth: `SiteAdmin`
- Code-behind: loads the project to get `ClientId`, then calls `IProjectService.GetForClientAsync(clientId)` to get all their projects
- Renders same list UI as `Portal/Projects.razor` (inline — no shared component needed, list page is simple)
- NavLinks to individual projects point to `/Admin/Projects/{projectId}/PortalPreview` (not the real portal), so all navigation stays within admin-preview routes
- If project not found: redirect to `/Admin/Projects`

---

## Data flow

```
Admin clicks "Portal view" on /Admin/Projects
  → /Admin/Projects/{Id}/PortalProjectsList
      IProjectService.GetByIdAsync(Id)        // get ClientId
      IProjectService.GetForClientAsync(...)  // get client's projects
      → render portal list UI
      → each project link → /Admin/Projects/{Id}/PortalPreview

Admin clicks "Preview as client" on /Admin/Projects/{Id}
  → /Admin/Projects/{Id}/PortalPreview
      IProjectService.GetByIdAsync(Id)        // no ownership check
      IInvoiceService.GetDownloadUrlAsync()   // presigned URLs (read-only S3)
      → PortalProjectDetailView renders project
```

---

## Safety

- Both pages are under `/Admin/` and require `SiteAdmin` role — a client user cannot access them
- No forms or event handlers that write to the database
- `PortalProjectDetailView` is a display-only component — it accepts no callbacks and performs no actions
- The real portal pages are unchanged — their ownership check remains in place
- Navigation from the projects list preview links to the admin preview detail, not the real portal

---

## Files changed / created

| File | Change |
|---|---|
| `Components/Shared/PortalProjectDetailView.razor` | **Create** — extracted view-only component |
| `Components/Pages/Portal/ProjectDetail.razor` | **Edit** — replace body with `<PortalProjectDetailView>` passthrough |
| `Components/Pages/Portal/ProjectDetail.razor.cs` | Minor edit if needed to expose fields to shared component |
| `Components/Pages/Admin/Projects/PreviewPortalDetail.razor` | **Create** |
| `Components/Pages/Admin/Projects/PreviewPortalDetail.razor.cs` | **Create** |
| `Components/Pages/Admin/Projects/PreviewPortalProjects.razor` | **Create** |
| `Components/Pages/Admin/Projects/PreviewPortalProjects.razor.cs` | **Create** |
| `Components/Pages/Admin/Projects/Detail.razor` | **Edit** — add "Preview as client" button |
| `Components/Pages/Admin/Projects/Index.razor` | **Edit** — add "Portal view" link in actions column |

---

## Out of scope

- Previewing other portal sections (Automations, Websites, Invoices, Dashboard) — can be added later using the same pattern
- Any "impersonate user" mechanism — this is a read-only data projection, not session switching
