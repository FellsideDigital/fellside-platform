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

There are no test projects in this solution.

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
