# S6 — Error Handling & User Feedback Design

**Date:** 2026-06-03
**Slice:** S6 (expanded from the audit backlog)
**Status:** Spec — awaiting approval. **Do not implement until approved.**

## 1. Goal

Make failures observable and recoverable everywhere:

1. **Server-side:** every meaningful failure is logged via `ILogger` with structured
   context. Raw exception text never reaches the user.
2. **Client-side:** every page that performs a form submit, a CRUD action, or an async
   data load gives the user clear, consistent feedback — inline error banners, transient
   toasts for action outcomes, empty states for "no data", and an error state for a failed
   load (with a retry affordance).

The result is one feedback vocabulary used across Marketing, Admin, and Portal.

## 2. Current state (what we're fixing)

- **Leaky errors:** 4 catch blocks put `ex.Message` straight into the on-screen alert
  (`Create.razor.cs:132`, `Edit.razor.cs:194`/`308`, `Invoices.razor.cs:105`). Internal
  detail leaks; nothing is logged.
- **Inconsistent logging:** `ILogger` is used in only a few services
  (`EmailService`, `InvitationService`, `S3StorageService`, `Scan`) and almost no pages.
- **No success feedback:** successful saves either silently redirect or set a one-off
  flag. There is no toast/notification system at all.
- **Ad-hoc load/empty handling:** `EmptyState` exists and is used on ~6 pages, but other
  list pages hand-roll "no data" markup, and **no** page handles a *failed* load — an
  exception in `OnInitializedAsync` tears down the circuit and shows the generic error UI.
- **No `ErrorBoundary`** anywhere; `Routes.razor` has `NotFound` but no render-error
  fallback.

## 3. Architecture

Three cooperating pieces, plus conventions.

### 3.1 Toast notifications (new, in `.UI`)

A per-circuit notification service and a host component.

- **`ToastService`** (scoped, registered in `ConfigurePortalServices`):
  - `void Success(string message, string? title = null)`
  - `void Error(string message, string? title = null)`
  - `void Info(string message, string? title = null)`
  - `void Warning(string message, string? title = null)`
  - Holds an `IReadOnlyList<Toast>` and raises `event Action? OnChange`.
  - `void Dismiss(Guid id)`.
- **`Toast`** record: `Id`, `Message`, `Title?`, `ToastLevel` (Success/Error/Info/Warning),
  `Duration` (default 4s; errors default 7s).
- **`ToastHost.razor`** (`.UI/Components/Feedback`): fixed-position stack (top-right on
  desktop, full-width top on mobile). Subscribes to `OnChange` and calls
  `InvokeAsync(StateHasChanged)` (Interactive Server safe). Each toast auto-dismisses via a
  `System.Threading.Timer`; manually dismissible. Reuses `AlertBanner`'s colour/icon
  vocabulary so toasts and inline banners look like one system. Implements `IDisposable`
  to unsubscribe and dispose timers.
- **Placement:** one `<ToastHost />` in each root layout — `MainLayout`, `AdminLayout`,
  `PortalLayout` — so every area is covered exactly once.

Rationale for toasts-as-service (not a cascading parameter): any code-behind can inject
`ToastService` and fire a notification without threading parameters through the tree.

### 3.2 Inline form & load errors (reuse `AlertBanner` + `EmptyState`)

- **Form/operation errors** continue to render through `AlertBanner Variant="error"` at the
  top of the form (already the pattern in `Create.razor`). We standardise the *message*
  (safe, friendly) and always pair it with a log.
- **Empty data** → `EmptyState` (already exists). Replace remaining hand-rolled "no data"
  blocks (e.g. the inline "No invoices for this client yet." in `Invoices.razor`).
- **Failed load** → a new lightweight pattern (see 3.4), surfaced with `AlertBanner` +
  a "Try again" button that re-runs the load.

### 3.3 Safe error messages + logging convention

- **`ErrorHandling` helper** (`FellsideDigital.Web`): a small static/utility that a catch
  block uses to (a) log the exception with context via the caller's `ILogger`, and
  (b) return a generic user-facing message. Signature sketch:
  `string LogAndDescribe(ILogger logger, Exception ex, string action)` →
  logs `"{Action} failed"` with the exception and returns e.g.
  `"Something went wrong while {action}. Please try again."`.
- **Where logging lives:** services log the *operational* failure (they own the DB/IO);
  pages log the *interaction* failure and decide what the user sees. Services should throw
  meaningful exceptions; pages translate them to user feedback. (We are **not** introducing
  a `Result<T>` pattern — with ~8 catch sites it adds ceremony without payoff. Exceptions +
  this helper are sufficient. This is an explicit YAGNI decision; revisit only if catch
  sites proliferate.)
- **Never** interpolate `ex.Message`/`ex` into user-facing strings.

### 3.4 Async load state pattern

A documented, page-local convention (no heavy framework):

```
enum LoadState { Loading, Loaded, Error }
```

Each data-loading page tracks `_loadState` and wraps its `OnInitializedAsync` load in
try/catch that logs and sets `LoadState.Error`. Render switches on it:

- `Loading` → spinner / skeleton (reuse the existing unused `LoadingScreen` or a simple
  inline spinner).
- `Error` → `AlertBanner` error + "Try again" button calling the load method.
- `Loaded` + empty collection → `EmptyState`.
- `Loaded` + data → content.

If during rollout this if/else proves repetitive enough, extract a single
`AsyncView` wrapper component (`Loading`/`ErrorContent`/`Empty`/`ChildContent`
fragments). Decision deferred to rollout — start with the convention, extract only if it
earns it.

### 3.5 Render-error boundary

Wrap the routed body in each root layout with `<ErrorBoundary>` providing a friendly
fallback (so a render exception shows a recoverable card instead of killing the circuit).
`System/Error.razor` remains the server-side unhandled-error page.

## 4. Coverage matrix

What each page gets. (F = inline form error, T = toast on action outcome, E = empty state,
L = load error/loading state, Log = server logging.)

| Page | F | T | E | L | Log |
|------|---|---|---|---|-----|
| Marketing/Contact (enquiry submit) | ✓ | ✓ (success+fail) | — | — | ✓ (enquiry service) |
| Marketing/Scan (QR lead) | ✓ | ✓ | — | — | ✓ (already) |
| Admin/Projects/Create | ✓ | ✓ | — | L (clients load) | ✓ |
| Admin/Projects/Edit (details + hero) | ✓ | ✓ | — | ✓ | ✓ |
| Admin/Projects/Index (list + delete) | — | ✓ (delete) | ✓ | ✓ | ✓ |
| Admin/Projects/Detail | — | ✓ (status update) | — | ✓ | ✓ |
| Admin/Projects/Updates | ✓ | ✓ | ✓ | ✓ | ✓ |
| Admin/Projects/PortalPreview | — | — | — | ✓ | ✓ |
| Admin/Invitations/Create | ✓ | ✓ | — | — | ✓ |
| Admin/Invitations/Index (resend/revoke/copy) | — | ✓ | ✓ | ✓ | ✓ |
| Admin/Clients/Invoices (add/delete/status) | ✓ | ✓ | ✓ | ✓ | ✓ |
| Admin/Enquiries/Index (mark read) | — | ✓ | ✓ | ✓ | ✓ |
| Admin/QrCampaign/Index | — | ✓ | ✓ | ✓ | ✓ |
| Portal/Settings (profile/password) | ✓ | ✓ | — | ✓ | ✓ |
| Portal/Index, Projects, Invoices, ProjectDetail, Automations, Websites | — | — | ✓ | ✓ | ✓ |

Static marketing pages (Home, Automation, Websites) need none.

## 5. Components & files

**New (`.UI`):**
- `Components/Feedback/Toast.cs` (record + `ToastLevel` enum)
- `Components/Feedback/ToastService.cs`
- `Components/Feedback/ToastHost.razor`

**New (`.Web`):**
- `ErrorHandling.cs` helper

**Modified:**
- `Extensions/ServiceConfigurationExtensions.cs` — register `ToastService` (scoped)
- `Components/Layout/{MainLayout,AdminLayout,PortalLayout}.razor` — add `<ToastHost />` and
  wrap body in `<ErrorBoundary>`
- The pages in §4 — adopt the patterns

## 6. Rollout phases (each builds green, committed separately)

1. **Infra:** Toast service + host + registration + layout wiring + `ErrorHandling` helper
   + `ErrorBoundary`. No page behaviour change yet. (Toasts demonstrable on one page.)
2. **Stop the leaks + log:** fix the 4 `ex.Message` catch sites to log + safe message +
   toast. Smallest, highest-value correctness fix.
3. **CRUD toasts:** Admin list pages (Invitations, Invoices, Enquiries, QrCampaign,
   Projects/Index delete; Projects/Detail status) get success/error toasts.
4. **Form pages:** Contact, Scan, Settings, Create/Edit standardise inline errors + success
   toasts.
5. **Load/empty states:** apply the `LoadState` pattern + `EmptyState` to all data-loading
   pages; replace hand-rolled empties. Extract `AsyncView` only if warranted.

## 7. Testing

Per the S7 decision (xUnit + Testcontainers Postgres), once the test project exists:
- `ToastService` unit tests (add/dismiss/auto-expire/event raised) — no DB needed.
- `ErrorHandling.LogAndDescribe` returns a safe message and logs (assert via a captured
  `ILogger`).
- Service-level failure paths covered as those services gain tests.
UI/render behaviour of `ToastHost`/`ErrorBoundary` is verified manually (run the app), since
we have no bUnit harness yet — adding bUnit is out of scope for S6.

## 8. Risks / call-outs

- **Interactive Server timing:** toast auto-dismiss timers fire off the render thread —
  must marshal via `InvokeAsync(StateHasChanged)`; host must dispose timers on circuit
  teardown to avoid leaks.
- **Verification gap:** toast/boundary visuals can only be confirmed by running the app.
  Builds verify compilation only; phase commits should be visually checked before release.
- **Scope discipline:** no `Result<T>`, no global state library, no bUnit — all explicitly
  deferred.

## 9. Out of scope

Retry/backoff policies, offline handling, telemetry/metrics beyond `ILogger`, and email/alert
escalation. These are not part of S6.
