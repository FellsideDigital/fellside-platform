# Client Project Page Redesign — Design

**Date:** 2026-06-02
**Status:** Approved (visual direction locked via brainstorming mockups `dashboard-v4`)

## Goal

Rebuild the client-facing project page (`/Portal/Projects/{Id}` → `PortalProjectDetailView`)
so it is genuinely useful: a calm, dashboard-style page that tells a client **where their
project is at**, lets them **preview their site**, and gives them their **documents** (an
admin-uploaded proposal and any other shared files). The current page surfaces a crude
3-step status bar and ignores the rich phase data that already exists; it is being replaced,
not patched.

The same shared component renders both the live portal page and the admin **Portal preview**
page, so this redesign serves both.

## Visual direction (locked)

Reference mockup: `.superpowers/brainstorm/<session>/content/dashboard-v4.html`. The agreed
look is **"less is more"**: borderless inline stat bar, hairline-bordered panels (no drop
shadows), no emoji/icons, a single accent colour, generous whitespace, uniform panels.

Page structure, top to bottom:

1. **Header** — back link, project name, status chip, type chip, "Visit live site / Open
   tool" button (only when a live URL exists). One muted line under the title carries the
   project `Description`.
2. **Inline stat bar** — four borderless stats separated by hairlines:
   - **Progress** — `ProgressPercent` with a thin progress bar.
   - **Current phase** — name of the active phase + "Phase _n_ of _N_".
   - **Target launch** — `TargetLaunchDate` + a derived "on track · in _x_ days" sub-line
     (omit the sub-line when no date is set).
   - **Outstanding** — sum of unpaid invoice amounts + the next due invoice's due date
     (shows "All settled" when nothing is outstanding).
3. **Hero preview** — full-width.
   - **Website** (`PreviewUrl` set): browser-chrome frame with the URL and an "Open ↗" link,
     containing the existing sandboxed `<iframe>`.
   - **Automation** (`ProjectUrl` set, no preview): the existing "Open your tool" card fills
     the same full-width slot.
   - Neither present: a quiet placeholder panel ("Preview coming soon").
4. **Lower grid** — two columns (`1.5fr / 1fr`):
   - **Project plan** (wide) — the `PlanPhases` rail (see below).
   - **Documents** then **Invoices**, stacked in the side column.

### Project plan rail

Driven by `ClientProject.PlanPhases` (ordered by `Order`). Each phase row shows: a status
dot (done = green, in-progress = accent ring, not-started = grey), the phase **Title**, and a
muted line combining target date + the client-facing message. The current (in-progress) phase
title is rendered in the accent colour.

- Client-facing per-phase message = `ImportantInformation` (fall back to `Notes` when empty).
- `InternalNotes` and `Dependencies` are **never** rendered on the client page.
- Date line: completed phases show "Completed _date_"; others show "Target _TargetCompletionDate_"
  (omit when null).

### Removed from this page

- The old 3-step `ProgressSteps` status bar.
- The "Project details" dl (Type/Started/About/Deployment notes). `Description` moves to the
  header sub-line; `DeploymentNotes` is treated as internal and is no longer shown to clients.
- The standalone **Activity / timeline** panel. The timeline feature still exists on the admin
  side (`Admin/Projects/Detail`, Notes) — only its appearance on the *client* page is dropped.

## Data model

### New: `ProjectDocument`

`Id, ProjectId (FK cascade), Title (string), FilePath (string — S3 object key),
FileName (string — original name for display), CreatedAt`

- `ClientProject.Documents : ICollection<ProjectDocument>`.
- DbContext config + one EF migration (`AddProjectDocuments`). No backfill.
- All documents on a project are client-visible by definition (the admin uploads them *for*
  the client). There is no internal-document concept here — internal files simply aren't
  uploaded. "Proposal" is just a document the admin titles "Project proposal"; no special
  type/enum (YAGNI).

No other schema changes — `ProgressPercent`, `TargetLaunchDate`, `PlanPhases`, `Invoices`
already exist.

## Services

### New: `IProjectDocumentService` (mirrors `InvoiceService` storage pattern)

Backed by the existing `IStorageService` (`UploadAsync(key, stream, contentType)`,
`GetPresignedUrlAsync(path, expiry)`, `DeleteAsync(path)`) and `StorageSettings.PresignedUrlExpiryMinutes`.

- `UploadAsync(projectId, title, IBrowserFile file)` — streams to S3 under a project-scoped
  key, persists the `ProjectDocument`, and records a `DocumentShared` timeline event
  (`ClientVisible`) — the enum value is already reserved, this wires it.
- `GetForProjectAsync(projectId)`.
- `GetDownloadUrlAsync(documentId)` — presigned URL (view/download).
- `DeleteAsync(documentId)` — removes S3 object + row (+ its event), mirroring invoice delete.

Register in `ServiceConfigurationExtensions`.

### `IProjectService`

- `GetByIdForClientAsync` and `GetByIdAsync`: add `.Include(p => p.Documents)` (and keep
  `PlanPhases` ordered). `GetByIdForClientAsync` already includes `PlanPhases`/`Invoices`.

## Frontend changes

- **`PortalProjectDetailView.razor`** — rewritten to the structure above. New code-behind
  helpers: current-phase resolution (first `InProgress`, else first non-`Completed`, else
  last), "phase n of N", days-to-launch, outstanding-balance sum + next due date. Reuses
  `BadgeHelpers` for status chips.
- **`ProjectDetail.razor.cs`** (portal) — also build presigned URLs for `Documents` into a
  separate `_documentUrls` (`Dictionary<Guid,string>`), mirroring the existing invoice
  `_downloadUrls` loop, and pass it to the view as a new parameter alongside `DownloadUrls`.
- **Admin document management** — new page **`/Admin/Projects/{Id}/Documents`** (consistent
  with the existing dedicated **Notes** page): upload (title + file), list with view/delete.
  Linked from the admin project Detail page alongside Notes. Admin-only.

## Edge cases / testing

- Project with no phases → stat bar Progress falls back to `Progress` field; plan panel shows
  an empty state ("Plan coming soon").
- No documents / no invoices → quiet per-panel empty states.
- No `TargetLaunchDate` → hide the launch sub-line; stat still shows "—".
- Presigned URL generation failure → document/invoice row renders without a working link
  (no page crash), matching current invoice behaviour.
- Visibility: clients only ever load via `GetByIdForClientAsync` (ownership checked in
  `ProjectDetail.razor.cs`); documents are inherently client-visible.
- No test projects in the solution → verify via `dotnet build` + manual admin upload →
  portal view walkthrough for both a website and an automation project.
