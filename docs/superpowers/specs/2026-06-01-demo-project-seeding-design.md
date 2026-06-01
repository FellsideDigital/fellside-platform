# Demo Project Seeding — Design

**Date:** 2026-06-01
**Goal:** Seed two clients and four projects (two website, two automation) on startup so the marketing hero carousel has real content to display in development.

## Context

- The hero carousel (`HeroProjectCarousel.razor`) is driven by `HeroProjectService.GetHeroProjectsAsync()`, which returns `ClientProject`s where `IsHeroProject == true`, ordered by `HeroDisplayOrder`, including `Metrics`, `PipelineSteps`, and `Integrations`.
- `ClientProject.ClientId` and `ClientProject.CreatedByAdminId` are **required** FKs (`OnDelete.Restrict`) to `ApplicationUser`.
- Existing seeding pattern: `AdminUserSeeder.SeedAdminAsync(IServiceProvider)`, invoked from `StartupCompositionExtensions.ApplyStartupTasksAsync`, idempotent via existence checks.
- The `AddHeroProjectFeatures` migration already provides the schema. No new migration required.

## Approach

New static seeder `DemoDataSeeder.SeedDemoProjectsAsync(IServiceProvider)` mirroring `AdminUserSeeder`:

- **Idempotency guard:** return early if `db.ClientProjects.AnyAsync()`.
- **Admin lookup:** find the admin user for `CreatedByAdminId` via `ADMIN_EMAIL`, falling back to the first user in the `SiteAdmin` role. If none, log and return (cannot satisfy required FK).
- **Clients:** create two `ApplicationUser`s via `UserManager` with confirmed emails + passwords (real login accounts):
  - `harbourline@demo.fellside.digital` — Harbourline Coffee Co.
  - `pennine@demo.fellside.digital` — Pennine Plant Hire
  - Dev password: `Demo!2026`
- **Wiring:** in `ApplyStartupTasksAsync`, after admin seeding, call the seeder only when `app.Environment.IsDevelopment()`.

## Seeded projects

All `Status = Completed`, `Progress = 100`, `IsHeroProject = true`. Interleaved display order for carousel variety. Each client owns one website + one automation project. `PreviewUrl` left null so website cards render the built-in wireframe rather than risking blocked iframes.

| Order | Client | Name | Type | Tagline |
|---|---|---|---|---|
| 0 | Harbourline Coffee Co. | Harbourline Storefront | Website | Headless storefront with same-day local delivery |
| 1 | Harbourline Coffee Co. | Wholesale Order Pipeline | Automation | Email orders to invoices, zero manual entry |
| 2 | Pennine Plant Hire | Pennine Booking Portal | Website | Self-service equipment booking & availability |
| 3 | Pennine Plant Hire | Fleet Maintenance Alerts | Automation | Predictive service reminders across the fleet |

### Metrics

- **Harbourline Storefront:** +38% Online sales (Up), 0.9s Load time (Speed), 4.9★ Avg rating (Neutral)
- **Wholesale Order Pipeline:** 6 hrs/wk Time saved (Warm), 100% Order accuracy (Up), <2 min Per order (Speed)
- **Pennine Booking Portal:** +52% Bookings (Up), 1.1s Load time (Speed), 24/7 Self-service (Neutral)
- **Fleet Maintenance Alerts:** 30% Less downtime (Warm), +18% Utilisation (Up), 0 Missed services (Neutral)

### Pipeline steps & integrations (automation projects)

- **Wholesale Order Pipeline:** Email inbox (Trigger) → Parse order (Process) → Sync stock (Process) → Xero invoice (Output). Integrations: Gmail, Xero, Shopify.
- **Fleet Maintenance Alerts:** Telematics (Trigger) → Check thresholds (Process) → Schedule job (Process) → SMS + calendar (Output). Integrations: Samsara, Google Calendar, Twilio.

## Out of scope

- Production seeding (dev-only).
- Screenshots / live iframe previews.
- Invoices, status updates, plan phases.
