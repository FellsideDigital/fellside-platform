# Project Timeline & Notes — Design

**Date:** 2026-06-02
**Status:** Approved

## Goal

Replace the admin "Updates" feature with: (1) internal project **Notes** (ServiceNow
work-note style, with per-note client visibility) and (2) a full project **Timeline** —
a chronological, newest-first feed of everything happening on a project, built on a
reusable, extensible event model.

## Decisions

- **Note visibility:** per-note toggle — `Internal` vs `ClientVisible`.
- **Timeline model:** materialized append-only event log (single source of truth).
- **Scope:** events for data that exists today (notes, invoices, project status, phases).
  File/document/task event types are reserved in the enum but not wired (no such
  subsystems exist yet) — they slot in later without rework.

## Data model

### `ProjectNote` (editable, admin-authored)
`Id, ProjectId (FK cascade), Body (text), Visibility (TimelineVisibility),
AuthorId (FK SetNull), AuthorName (snapshot), CreatedAt, UpdatedAt`

### `ProjectTimelineEvent` (append-only log)
`Id, ProjectId (FK cascade), Type (TimelineEventType), OccurredAt,
ActorId (FK SetNull, nullable), ActorName (snapshot), Visibility (TimelineVisibility),
Summary (snapshot text — frozen, auditable), NoteId (FK SetNull, nullable — note events
only, lets timeline render live note body), Data (string? JSON for extra structured detail)`

### Enums (Domain)
- `TimelineVisibility { Internal, ClientVisible }`
- `TimelineEventType { ProjectCreated, StatusChanged, PhaseChanged, MilestoneCompleted,
  ProjectCompleted, ProjectReopened, NoteAdded, InvoiceCreated, InvoiceSent, InvoicePaid,
  InvoiceOverdue, InvoiceViewed, FileUploaded, DocumentShared, TaskCreated, TaskCompleted }`
  (last four reserved/not wired).

### Removed
`ProjectStatusUpdate` entity, DbSet, config, and `ClientProject.StatusUpdates`. Replaced
by `ClientProject.Notes` and `ClientProject.TimelineEvents`.

## Services

- **`IProjectTimelineService`**: `RecordAsync(projectId, type, summary, visibility, actorId?,
  noteId?, data?)` (resolves ActorName from the users table), `GetForProjectAsync`. Single
  write/read chokepoint.
- **`IProjectNoteService`**: `AddAsync / UpdateAsync / DeleteAsync / GetForProjectAsync`.
  Add → also records a `NoteAdded` event (visibility mirrors the note). Delete → removes the
  note and its event. Edit → updates the note (body shown live via `NoteId`).
- **Emission wired into existing services:**
  - `ProjectService.CreateAsync` → `ProjectCreated`.
  - `ProjectService.UpdateAsync(project, actorId?)` → diffs persisted vs new `Status`
    (AsNoTracking read of original) → `StatusChanged` / `ProjectCompleted` / `ProjectReopened`.
  - `ProjectService.SavePhasesAsync(projectId, phases, actorId?)` → diffs old vs new phases by
    Title → `PhaseChanged` / `MilestoneCompleted`.
  - `InvoiceService.UploadAsync(..., actorId?)` → `InvoiceCreated`.
    `UpdateStatusAsync(id, status, actorId?)` → `InvoicePaid` / `InvoiceOverdue` / `InvoiceSent`.
  - `InvoiceViewed`: type reserved, not wired (no view tracking exists).
- **Loads:** admin `GetByIdAsync` includes all events + notes; new `GetByIdForClientAsync`
  and `GetForClientAsync` include only `ClientVisible` events (filtered include). Defence in
  depth: the timeline component also filters by `Audience`.

## Frontend

- **`TimelineEventPresenter`** (static): maps `TimelineEventType → { IconPath, ToneClasses }`
  in one place — no per-type switch in Razor. Description uses the event's `Summary`.
- **`<ProjectTimeline>`** component: takes events + `Audience`, renders newest-first; shows
  live note body for note events.
- **Admin Notes page** (`/Admin/Projects/{Id}/Notes`, replaces the Updates route): add /
  edit / delete notes with the Internal/Client-visible toggle; lists notes chronologically
  with author + timestamp.
- **Replaces** the activity/updates markup in admin `Detail`, portal `SingleProjectOverview`,
  `MultiProjectOverview`, `Index`, `Projects`, `PortalProjectDetailView`.
- Admin status changes happen on the **Edit** page (already there); `UpdateAsync` emits the
  event. No separate quick-status control.

## Migration + backfill (one EF migration, SQL in `Up()` before dropping the old table)

- Each `ProjectStatusUpdate` with a message → a `ClientVisible` `ProjectNote` + `NoteAdded` event.
- Each with `NewStatus` → a `StatusChanged` event at its `CreatedAt`.
- Backfill `ProjectCreated` per project; `InvoiceCreated`/`InvoicePaid` per invoice;
  `MilestoneCompleted` per completed phase. Then drop `ProjectStatusUpdates`.

## Edge cases / testing

- User/invoice/phase deletion: events survive via snapshots + `SetNull` FKs.
- Visibility enforced server-side; clients never receive internal events.
- No test projects in the solution → verify via `dotnet build` + manual admin/portal walkthrough.
