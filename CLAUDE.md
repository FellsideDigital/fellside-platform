# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

**Run in development:**
```bash
dotnet run --launch-profile http
# App at http://localhost:5185
```

**Run with Docker (full stack, includes PostgreSQL):**
```bash
docker-compose up --build
# App at http://localhost:8080
```

**Run with Docker (production, requires external DB):**
```bash
docker-compose -f docker-compose.prod.yml up --build
```

**Database migrations:**
```bash
dotnet ef migrations add <MigrationName> --project src/FellsideDigital.Web
dotnet ef database update --project src/FellsideDigital.Web
```

**Tailwind CSS** — runs automatically on `dotnet build` via MSBuild targets, but to run manually:
```bash
cd src/FellsideDigital.Web
npm install
npx tailwindcss -i ./Styles/tailwind.css -o ./wwwroot/css/tailwind.css
```

**Run tests:**
```bash
dotnet test
```
Tests live in `tests/FellsideDigital.Tests` (xUnit). Service/data tests use **Testcontainers** to spin up a real PostgreSQL container, so **Docker must be running** to execute them.

## Required Environment Variables

| Variable | Purpose |
|---|---|
| `ADMIN_EMAIL` | Seeds the initial admin user on first boot |
| `ADMIN_PASSWORD` | Password for the seeded admin user |
| `DATABASE_URL` | Railway-style `postgres://` URI (overrides connection string) |
| `ConnectionStrings__DefaultConnection` | Standard .NET connection string (alternative to DATABASE_URL) |

## Architecture

### Solution Layout

The solution is split into three projects under `src/`:

- `FellsideDigital.Domain` — framework-free enums and extensions (e.g. enum display-name helpers).
- `FellsideDigital.UI` — the shared Razor component library (buttons, cards, feedback, navigation). New reusable UI primitives belong here, not in `.Web`.
- `FellsideDigital.Web` — the Blazor Server app: pages, layouts, services, data layer, and startup composition.

### Startup Composition Pattern

`Program.cs` is intentionally thin — it wires data-protection key persistence, then delegates service registration to `AddFellsideDigitalPlatform()`, startup tasks to `ApplyStartupTasksAsync()`, and the middleware pipeline to `UseFellsideDigitalPlatform()`. Everything else lives in `Extensions/`:

- `StartupCompositionExtensions.cs` — orchestrates the other extensions; also contains `ApplyStartupTasksAsync()` (runs migrations + admin seeding) and `UseFellsideDigitalPlatform()` (middleware pipeline)
- `AuthenticationExtensions.cs` — ASP.NET Identity config, cookie settings, registers `IdentityNoOpEmailSender`
- `DatabaseExtensions.cs` — PostgreSQL via Npgsql; parses Railway's `DATABASE_URL` env var format
- `ServiceConfigurationExtensions.cs` — HTTP context, form options (50 MB body limit), data protection, session, logging

New services should be added as extension methods in this `Extensions/` folder and called from `AddFellsideDigitalPlatform()`.

### Authentication & Roles

- Two roles: `SiteAdmin` and `AuctionAdmin`, created automatically by `AdminUserSeeder` at startup when `ADMIN_EMAIL`/`ADMIN_PASSWORD` env vars are present.
- `RequireConfirmedAccount = true` is set, but `IdentityNoOpEmailSender` is registered — **no emails are actually sent**. In development, `RegisterConfirmation.razor` renders a clickable confirmation link directly on screen.
- Cookie auth: 14-day sliding expiration, `SameSite=Lax`, always `Secure`. API routes (`/api/*`) return 401/403 JSON instead of redirecting.
- Security stamps are revalidated every 30 minutes via `IdentityRevalidatingAuthenticationStateProvider`.

### Data Layer

- `FellsideDigitalDbContext` extends `IdentityDbContext<ApplicationUser>`. The `Customers` DbSet is a semantic alias for the Identity users table.
- `ApplicationUser` currently has no custom properties beyond the base `IdentityUser`.
- Migrations apply automatically on startup — no manual `dotnet ef database update` needed in normal operation.
- `Components/Pages/Marketing/Home.razor.model.cs` contains hardcoded static data for the landing page (projects, services, testimonials, FAQs) — this is not database-driven.

### Blazor Rendering

- All components use **Interactive Server** render mode. The router is in `Routes.razor` with `MainLayout` as the default layout.
- Admin pages use `AdminLayout` and portal pages use `PortalLayout`; both are under `Components/Layout/`.
- The landing page (`/`) delegates scroll and entrance animations to JavaScript (Anime.js) via JS interop on first render.

## Conventions for New Work

**Read this before adding any feature.** These patterns are established across the codebase — follow them rather than inventing new ones, and prefer extending an existing component/service over creating a parallel one.

### 1. UI component library first

Before writing raw HTML for a common element, use (or extend) a `.UI` component. Reusable UI belongs in `FellsideDigital.UI`, **not** in `.Web`. If you add a new `.UI` namespace folder, register its `@using` in `src/FellsideDigital.Web/Components/_Imports.razor`.

| Need | Use | Namespace |
|------|-----|-----------|
| Form input styling | `FieldStyles.Input` / `.TextArea` / `.Error` (class constants) | `UI.Components.Forms` |
| Labelled field (label + required + hint + validation) | `FormField` | `UI.Components.Forms` |
| Data tables | `Th`, `Td`, `TableStyles` (`.Table`/`.HeadRow`/`.Body`/`.Row`) | `UI.Components.Tables` |
| Status pill | `StatusBadge` (optional `Dot` for a status dot) | `UI.Components.Feedback` |
| Empty list state | `EmptyState` | `UI.Components.Feedback` |
| Inline alert | `AlertBanner` (`Variant`: success/error/warning/accent) | `UI.Components.Feedback` |
| Modal dialog | `Modal` (backdrop + panel shell) — compose content as `ChildContent` | `UI.Components.Feedback` |
| Transient feedback | `ToastService` + `ToastHost` (see §3) | `UI.Components.Feedback` |
| Buttons | `PrimaryButton`, `SpinnerButton` | `UI.Components.Buttons` |

Don't re-declare local `InputClass` Tailwind strings or hand-roll `<th>`/`<td>` class strings — use the constants above.

### 2. Business logic lives in services, not components

- Pages/components **must not** inject `FellsideDigitalDbContext` or contain data-access logic. Put it in a service under `src/FellsideDigital.Web/Services/` behind an `I…Service` interface, and register it in `ServiceConfigurationExtensions.ConfigurePortalServices`.
- Code-behind (`*.razor.cs`) orchestrates only: call services, map to/from view-models, manage UI state. Keep view-models out of the main code-behind where it grows large (see `Edit.razor.Models.cs` for the partial-class pattern).

### 3. Error handling & user feedback (required for every form / CRUD / action)

- **Never** surface `ex.Message` or exception detail to users. Wrap risky operations in `try/catch` and call `ErrorHandling.LogAndDescribe(Logger, ex, "doing X")` — it logs the real exception via `ILogger` and returns a safe, friendly message. Inject `ILogger<TPage>`.
- Inject `ToastService` and call `Toasts.Success(...)` / `Toasts.Error(...)` for action outcomes (delete, status change, save-and-navigate). A `ToastHost` is already mounted in all three layouts.
- Use `AlertBanner` for inline form/validation errors that should stay near the form; use **toasts** for transient action outcomes.
- A service may throw `InvalidOperationException` carrying a deliberate user-facing validation message (e.g. "file too large"); catch that specifically and show its message, but fall back to `LogAndDescribe` for unexpected exceptions.
- Every layout wraps the routed body in `<ErrorBoundary>` with `ErrorFallback`, so an unhandled render/load failure shows a recoverable card instead of dropping the circuit.

### 4. Helpers

- `ListEditing` (`FellsideDigital.Web`) — index-safe `MoveUp`/`MoveDown`/`RemoveAt` for in-place list editors.

### 5. Testing

When you add or change a service / business logic, add or extend tests in `tests/FellsideDigital.Tests`. DB-backed tests derive from `[Collection("postgres")]` and use `PostgresFixture.CreateContext()` (Testcontainers — Docker required). Pure-logic tests (e.g. `ToastService`, `ErrorHandling`) need no fixture.

Design rationale for larger refactors is recorded under `docs/superpowers/specs/`.

## Working Principles & Security Policy

This section is the single source of truth for how work is done and the security bar every change must clear. Where any other note conflicts with the verified code, the code wins — these rules are written to match the codebase as it actually is.

### Core principles

| Principle | Rule |
|-----------|------|
| **Simplicity first** | The simplest correct solution wins. No unnecessary abstraction. |
| **No laziness** | Find root causes. Never apply band-aid fixes. |
| **Minimal impact** | Only touch what the task requires. No drive-by refactors. |
| **Verify before done** | Never mark a task complete without proving it works. |

### Workflow

- **Features / changes with 3+ steps:** write a short plan first (goal, steps, risks, success criteria), confirm it makes sense, track progress as you go, and STOP to re-plan if something goes wrong mid-task rather than pushing through uncertainty.
- **Bug reports:** just fix them — no check-in needed. Reproduce and understand the bug, find the root cause (not the symptom), fix with minimal blast radius, verify, then report what was wrong and what changed.

(The plan-first rule applies to new/feature work; the fix-directly rule applies to bug reports.)

### Security rules (non-negotiable on every change)

- **Never hardcode secrets.** Credentials, connection strings, and S3 keys come from environment variables or bound settings (`StorageSettings`, `EmailSettings`, `ConnectionStrings__DefaultConnection`, `DATABASE_URL`) — never literals in code, not even in tests.
- **Always use parameterised queries.** The data layer is **EF Core only** (no Dapper, no raw SQL). Do not introduce `FromSqlRaw`/`ExecuteSqlRaw` with string concatenation; if raw SQL is ever truly needed, parameterise it.
- **S3 is private.** All blob access goes through `IStorageService` — reads/writes via presigned URLs (`GetPresignedUrlAsync`), never public bucket ACLs. Bucket privacy and lifecycle rules are infra config (see "Out of code scope" below).
- **Auth & session cookies** are `HttpOnly` + `Secure` (`CookieSecurePolicy.Always`) with **`SameSite=Lax`** — this is deliberate; `Lax` is required so redirect-back flows (external login return, email-link landings) keep the cookie. Do **not** change auth/session cookies to `Strict`. `SameSite=Strict` is used only for the non-navigational status-message cookie in `IdentityRedirectManager`.
- **No exception detail to users.** This is a Blazor Server app: catch risky operations and use `ErrorHandling.LogAndDescribe(Logger, ex, "doing X")` (logs the real exception, returns a safe message); every layout already wraps the routed body in `<ErrorBoundary>` with `ErrorFallback`. API routes under `/api/*` return safe 401/403 JSON. There is no generic "global exception middleware" pattern — use the existing `ErrorHandling` + `ErrorBoundary` mechanism.
- **Every protected route needs `[Authorize]`.** Audit any new page/endpoint for the correct role (`SiteAdmin` / `AuctionAdmin`) before marking work done.

### Auth specifics (as configured in `AuthenticationExtensions.cs`)

- Account lockout is active: 5 failed attempts → 15-minute lockout. Keep it on.
- Password policy: 12-char minimum, requires upper/lower/digit/non-alphanumeric. Don't weaken it.
- 2FA is supported (`LoginWith2fa`, recovery codes) — don't break it.
- **Email is no-op in normal operation:** identity confirmation / password-reset go through `IdentityNoOpEmailSender`, so those emails are **not delivered** (dev renders the confirmation link on screen). A separate transactional `EmailService` exists for app emails. If you build a flow that depends on a delivered identity email, wire a real `IEmailSender` first — don't assume reset/confirm emails currently send.

### Database (PostgreSQL via EF Core)

- App connects with a least-privilege user — never the superuser.
- All inputs parameterised (EF Core handles this; don't bypass it).
- Add indexes when introducing new `WHERE`/join columns.
- Never run a destructive migration without a rollback plan. Migrations apply automatically on startup.

### Code style

- Follow existing conventions — don't introduce new patterns without reason (see "Conventions for New Work" above).
- Remove dead code / unused imports you touch as part of a task.
- Log via the injected `ILogger` (auth, errors, S3 ops) — never `Console.WriteLine`.
- Keep methods small and single-purpose.

### What NOT to do

- Don't refactor code outside the task.
- Don't upgrade packages unless asked.
- Don't expose internal error detail to API consumers.
- Don't commit secrets, even test ones.
- Don't assume infrastructure — ask if unsure.

### Out of code scope (infra — can't be verified from this repo)

These are real requirements but live in infra/IAM/bucket config, not in code, so a code review can't confirm them — treat as ops checklist items, not code-change gates:

- S3 bucket privacy (no public ACLs) and lifecycle rules for temp/unused objects.
- Least-privilege DB grants for the app user.

### Definition of done

- [ ] Code compiles and runs without errors.
- [ ] The specific requirement is met and tested.
- [ ] No secrets hardcoded.
- [ ] No new security holes; auth/cookie/query rules above upheld.
- [ ] Existing functionality not broken.
- [ ] Changes summarised clearly.
