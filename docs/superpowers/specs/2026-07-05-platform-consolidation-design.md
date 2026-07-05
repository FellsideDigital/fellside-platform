# Platform Consolidation — Design

**Date:** 2026-07-05
**Status:** Approved by Oliver (interactive session)

## Goal

One pass over the whole app to: unify the email system, guarantee password-change
access for every user type, fix the flaky home-page project iframes, enforce and
expand the reusable `.UI` component library across all pages, fix concrete
admin/portal design defects, and verify SEO. The location landing pages
(`/website-design/{Town}`) are confirmed as a deliberate local-SEO strategy and are
kept as-is (verification only).

Visual output is preserved everywhere — workstreams D/E are refactors and defect
fixes, not a redesign.

## Verified findings this design responds to

1. `EmailService` (Microsoft Graph) is registered as `IEmailSender<ApplicationUser>`
   (`AuthenticationExtensions.cs:85`) and already sends **all** mail, identity flows
   included. `IdentityNoOpEmailSender` is dead code except a stale type-check branch
   in `RegisterConfirmation.razor` that can no longer trigger. CLAUDE.md still claims
   identity emails don't send — stale. Gap: with no email credentials configured
   (typical local dev), registration throws instead of showing the dev confirmation
   link.
2. Password change exists at `/Portal/Settings` (clients) and
   `/Account/Manage/ChangePassword` (identity). `UserMenu.razor` is shared by
   `AdminLayout` and `PortalLayout` but hard-links Settings to `/portal/settings`,
   which is wrong for admins. Manage pages hand-roll input CSS.
3. Hero carousel iframe race: `hero-carousel.js#tryLoadIframe` is invoked from
   `OnAfterRenderAsync`, hides the iframe, and waits for its `load` event. If the
   iframe loaded before the listener attached (fast site, or any re-render re-running
   the interop on an already-loaded iframe), `load` never fires and the 6 s deadline
   forces the screenshot fallback. This is the "hosted and working but flaky" bug.
4. Component-library deviations:
   - `Admin/Projects/Documents.razor.cs` and `Notes.razor.cs` duplicate the raw
     `FieldStyles.Input` Tailwind string verbatim.
   - `Marketing/Contact.razor.cs` and `Scan.razor.cs` each define their own
     (different) marketing input styles.
   - `Portal/Overview/MultiProjectOverview.razor` hand-rolls `<th>`.
   - Portal `Settings.razor`, identity Manage pages, and others hand-roll
     label+input blocks instead of `FormField`.
   - `UserMenu.razor` has a colourless `border-b`, mixed `gray-*`/`neutral-*`
     palettes, and inline link styles.
5. SEO: all marketing page types carry `SeoHead` + JSON-LD; robots.txt and
   sitemap.xml are coherent (4 towns in model = 4 in sitemap). Gaps: no default
   `og:image`, Search Console verification token commented out (owner action),
   sitemap `lastmod` needs bumping on release.

## Workstreams

### A. Email unification

- Delete `IdentityNoOpEmailSender` and the `EmailSender is IdentityNoOpEmailSender`
  branch in `RegisterConfirmation.razor`.
- Add `EmailSettings.IsConfigured` (true when TenantId/ClientId/ClientSecret/
  FromAddress are all present).
- `RegisterConfirmation.razor` renders the on-screen confirmation link when
  `!EmailSettings.IsConfigured` — restores the local-dev experience keyed off
  configuration instead of sender type. `EmailService.SendAsync` keeps throwing when
  unconfigured so production misconfiguration stays loud.
- Extract `IEmailService : IEmailSender<ApplicationUser>` exposing the custom send
  methods; register `EmailService` behind it; all consumers (services, pages) inject
  the interface. Registration stays in one place.
- Update CLAUDE.md's stale email notes (identity emails DO send via Graph).

### B. Password change for everyone

- `UserMenu` receives the correct Settings target from its hosting layout
  (admin → `/Account/Manage`, portal → `/Portal/Settings`) — parameter, not
  hard-coded path.
- Restyle identity Manage pages (`ChangePassword`, `Index`, etc.) onto
  `FieldStyles`/`FormField` so they match the rest of the app.
- Verify end-to-end: portal change-password, identity change-password,
  forgot-password email flow (now real via Graph).

### C. Hero iframe reliability

- Fallback (screenshot/wireframe) visible by default in markup; iframe hidden by
  default — no JS needed for the initial state.
- Detect `load` race-proof: a JS-side registry keyed by iframe id records `load`
  events via a listener attached through an inline `onload` attribute (present
  before the iframe can possibly load), so late interop can query "already loaded?"
  instead of waiting for an event that already fired.
- Idempotent per project: once a project's preview loaded successfully, revisiting
  its slide shows the iframe immediately (no repeated 6 s probation).
- Keep the 6 s deadline + error fallback for genuinely unframeable sites
  (X-Frame-Options / CSP).

### D. Component sweep (largest)

- `FieldStyles` gains a `MarketingInput` constant capturing the Contact/Scan look;
  both pages use it (their two variants converge on one).
- `Documents.razor.cs` / `Notes.razor.cs` reference `FieldStyles.Input` instead of
  duplicated literals.
- `MultiProjectOverview.razor` uses `Th`/`Td`/`TableStyles`.
- Hand-rolled label+input blocks convert to `FormField` where the markup is the
  standard pattern (Portal Settings, Manage pages, admin forms).
- Extract new `.UI` components for markup repeated 2+ times. Confirmed candidates:
  - `BrowserFrame` (browser-chrome preview shell used by hero carousel; check
    Websites page for a second instance).
  - `Breadcrumb` (marketing location pages + any admin usage).
  - `MetricTile` (hero carousel metrics; check portal overview).
  - `MenuItem`/dropdown item (UserMenu links).
  Final list is set during implementation by verifying each candidate genuinely
  repeats; no speculative components (YAGNI).
- New `.UI` namespace folders get `@using` entries in `.Web/_Imports.razor`; the
  CLAUDE.md component table is updated.

### E. Admin & portal polish

- Fix `UserMenu` styling (border colour, one palette, shared item styles).
- Page-by-page pass over all Admin and Portal pages checking: dark-mode class
  coverage, `EmptyState` for empty lists, toast/alert usage per the error-handling
  convention, mobile responsiveness of tables/grids. Fix concrete defects found;
  log anything ambiguous rather than redesigning.

### F. SEO verification

- Verify per page: `PageTitle`, `SeoHead` canonical/description, JSON-LD validity
  (Home LocalBusiness, location Service/Breadcrumb/FAQ schemas).
- Add a default `og:image` (existing 512 px brand asset) to `SeoHead` so every page
  has one unless overridden.
- Bump sitemap `lastmod` to release date; confirm route↔sitemap parity (public
  marketing routes only; `/scan` stays excluded per robots.txt).
- Deliverable: short owner checklist for items outside code (Search Console token,
  Google Business Profile).

## Approach

Single branch `feature/platform-consolidation`, one commit per workstream, order
A→F. Alternatives considered: six separate PRs (rejected — overlapping files,
review overhead) and minimal-fixes-only (rejected — user explicitly wants the full
component sweep).

## Error handling

Existing conventions apply unchanged: `ErrorHandling.LogAndDescribe` for risky ops,
toasts for action outcomes, `AlertBanner` for inline form errors, no exception
detail to users. Email failures continue to propagate to callers (no silent
swallowing); the only behaviour change is the dev-mode unconfigured path in A.

## Testing

- Unit tests updated/added for: `EmailSettings.IsConfigured`, any service whose
  signature changes with `IEmailService`.
- Existing suite must stay green (`dotnet.exe test`; Docker-backed tests when
  Docker is up).
- Manual visual verification by Oliver via VS Docker rebuild (app on :8080) after
  each workstream lands, especially C (iframe) and E (portals).

## Success criteria

1. No references to `IdentityNoOpEmailSender` remain; all email goes through
   `IEmailService`; dev registration shows on-screen link when email unconfigured.
2. Admin and portal users each reach a working change-password page from their own
   layout's menu; forgot-password sends a real email.
3. Home-page website previews load reliably on repeat visits and carousel
   navigation; fallback only appears for genuinely unframeable sites.
4. No page declares raw input/table Tailwind literals that duplicate `.UI`
   constants; repeated markup ≥2 instances is a `.UI` component.
5. Admin/portal defect list fixed; dark-mode consistent.
6. SEO checklist complete; owner-action items handed over.
