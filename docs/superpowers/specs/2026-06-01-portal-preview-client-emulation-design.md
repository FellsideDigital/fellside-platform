# Client Portal Preview via Client-Id Override — Design

**Date:** 2026-06-01
**Status:** Approved (pending spec review)

## Problem

The current admin "portal preview" is two bespoke admin-only pages
(`PreviewPortalProjects`, `PreviewPortalDetail`) that only partially reproduce
the client portal (projects list + project detail). They are a mock: they do not
show the Overview or Invoices the client actually sees, and they drift from the
real portal whenever the real portal changes.

We want admins to preview the **real** `/Portal/*` experience exactly as a chosen
client sees it, by overriding the client id the portal resolves — not by
maintaining a parallel set of admin pages.

## Goals

- An admin can view the genuine client portal pages (Overview, Projects, Project
  detail, Invoices) as a specific client.
- "Exact emulation": the real portal components and routes render — no
  duplicated/forked UI.
- The preview state is obvious on screen and easy to exit.
- Remove the old mock pages entirely.

## Non-goals

- Settings is **not** impersonated. It edits the logged-in account
  (name/password via `UserManager`); routing it to a client would risk an admin
  editing the client's account. In preview it continues to show the admin's own
  account.
- No persistence across a hard browser refresh (see Persistence below).

## Decisions (from brainstorming)

- **Preview scope:** data pages only — Overview, Projects, Project detail,
  Invoices. Settings excluded.
- **Enter/exit UX:** admin clicks "Preview portal" on a project → lands on the
  real `/Portal` as that client → a persistent banner reads
  *"Previewing as {Client} — Exit preview"*; Exit clears state and returns to
  admin.

## Architecture

### 1. `PortalPreviewState` — new scoped service

Lives for the Blazor circuit (Interactive Server). Holds:

- `Guid? PreviewClientId`
- `string? PreviewClientName`
- `Guid? SourceProjectId` (where the admin launched from, for the Exit return)

API:

- `void Enter(string clientId, string clientName, Guid sourceProjectId)`
- `void Exit()`
- `bool IsActive` (true when `PreviewClientId` is set)
- `string ResolveClientId(string currentUserId, bool isSiteAdmin)` — returns the
  preview client id when `IsActive` **and** `isSiteAdmin`; otherwise returns
  `currentUserId`. Single central place; no duplicated resolution logic.

Registered as `AddScoped<PortalPreviewState>()` in
`ServiceConfigurationExtensions` (or the relevant existing registration
extension).

### 2. Launcher page — `Admin/Projects/{Id:guid}/PortalPreview`

Replaces both mock pages and reuses the existing entry-point route name so the
links in `Detail.razor` and `Index.razor` need only minimal change.

- `@attribute [Authorize(Roles = "SiteAdmin")]`, no layout/visible UI.
- `OnInitializedAsync`: load the project by `Id`; if missing → redirect to
  `/Admin/Projects`. Resolve the client's display name (from the project's client
  user — first/last name, falling back to email). Call
  `PortalPreviewState.Enter(project.ClientId, clientName, project.Id)`. Redirect
  to `/Portal`.

### 3. Portal data pages — resolve via the override

`Index` (`/Portal`), `Projects` (`/Portal/Projects`), `ProjectDetail`
(`/Portal/Projects/{Id}`), `Invoices` (`/Portal/Invoices`):

- Inject `PortalPreviewState`.
- Determine `isSiteAdmin` from the auth state (role check).
- Replace the `user.Id` / NameIdentifier claim they pass to services with
  `PreviewState.ResolveClientId(currentUserId, isSiteAdmin)`.
- `ProjectDetail`'s ownership check changes from `_project.ClientId != user.Id`
  to compare against the resolved client id, so the admin can open the client's
  project.

`Settings` is untouched.

### 4. Banner — in `PortalLayout`

- When `PortalPreviewState.IsActive`, render a strip above the topbar:
  *"Previewing as {PreviewClientName} — Exit preview"*.
- Exit button calls `Exit()` then navigates to
  `/Admin/Projects/{SourceProjectId}`.
- Inject `PortalPreviewState` into `PortalLayout`.

### 5. Deletions

- `Components/Pages/Admin/Projects/PreviewPortalProjects.razor` + `.razor.cs`
- `Components/Pages/Admin/Projects/PreviewPortalDetail.razor` + `.razor.cs`
- The `BackHref` / `BackLabel` arguments those passed to the shared
  `PortalProjectDetailView` are dropped (the real `ProjectDetail` already calls
  the shared view without them, so the component is unaffected).

### Entry-point link updates

- `Admin/Projects/Detail.razor` (the "preview" button) → point at
  `/Admin/Projects/{Id}/PortalPreview`.
- `Admin/Projects/Index.razor` (the row action) → same route.
- Both currently point at `/Admin/Projects/{Id}/PortalProjectsList` (deleted).

## Persistence

`PortalPreviewState` is scoped to the circuit, so it survives all in-app
navigation (Overview → Projects → Detail → Invoices). A hard browser refresh
starts a new circuit and clears it, dropping the admin back to their own account.
This is acceptable: the banner makes the active state obvious, so preview is
never silent, and "refresh exits preview" is intuitive. No cookie/session
storage is introduced (avoids async-init churn across every portal page).

## Security

- Only the admin-only launcher calls `Enter`, so a normal client never has
  preview state set.
- Defense in depth: `ResolveClientId` only honors the override when the current
  user is in the `SiteAdmin` role, so even a stray set could not leak another
  client's data to a non-admin.

## Testing

No test projects exist in the solution (per CLAUDE.md). Verification is manual:

1. As `SiteAdmin`, open a project's admin page → click Preview portal → land on
   `/Portal` showing that client's overview; banner visible.
2. Navigate Overview → Projects → a project → Invoices: all show the client's
   data; banner persists.
3. Exit preview → returns to `/Admin/Projects/{project}`; visiting `/Portal`
   again shows the admin's own (empty) account, no banner.
4. Confirm a normal client logging in sees only their own data (no regression).
