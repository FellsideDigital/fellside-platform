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
