# Live Automation Showcase — Design

**Date:** 2026-07-13
**Status:** Approved (brainstorming)

## Goal

Bring the standalone "Live Automation Showcase" (`FellsideDigital/Presentation`) into the
Fellside Digital site as a first-class, Blazor-native feature. At an event, a big screen
displays a QR code, a live participant count, an animated automation pipeline, and a live
feed. Audience members scan the QR on their phones, submit their name + email, and the big
screen reacts in real time while a branded "you just triggered a live automation" email is
sent. Captured people become real leads in the existing admin QR dashboard.

The standalone app is **re-expressed** in the site's stack — nothing is ported verbatim.

## What already exists in the site (reused, not rebuilt)

- `QrLead` / `QrScan` entities, `IQrLeadService` / `QrLeadService`, admin QR-campaign
  dashboard (`Components/Pages/Admin/QrCampaign`).
- `IEmailService` / `EmailService` (Microsoft Graph) with the `EmailTheme` / `EmailTemplates`
  branded-email system (inline logo via `cid:fellside-logo`).
- Compiled Tailwind + design tokens (`accent`, light/dark mode), the `.UI` component library,
  `ErrorHandling.LogAndDescribe`, `ToastService`, `ErrorBoundary` per layout.
- Blazor Server (Interactive Server render mode) — SignalR circuits are already the transport.

## Mapping: standalone → site

| Standalone showcase | Site implementation |
|---|---|
| Raw SignalR `Hub` + `signalr.js` CDN | Blazor Server circuits + singleton `LiveShowcaseState` broadcaster |
| `screen.js` / `join.js` | Two Blazor Interactive Server pages; animation driven by C# state |
| CDN Tailwind | Site's compiled Tailwind + design tokens, light/dark-aware |
| In-memory `ParticipantStore` | Real `QrLead` persistence (source `"live"`) + in-memory `LiveShowcaseState` for the live view |
| `GraphEmailSender` | Existing `EmailService` + one new template |
| `CompanyResolver`, `EmailValidator` | Ported as helpers under `Services/` |
| `QrCoder` SVG endpoint | `QRCoder` NuGet + `/api/live/qr.svg` minimal-API endpoint |

## Decisions (from brainstorming)

1. **Full live experience** — cross-device big screen + phone, real-time.
2. **Leads persist** as `QrLead` (source `"live"`) → visible in existing admin dashboard.
3. **New branded email** — "You just triggered a live automation ⚡", built on `EmailTheme`.
4. **Big screen is admin-only** (`[Authorize(Roles="SiteAdmin")]`); join page is public.
5. **QRCoder** NuGet added for server-side QR SVG.
6. **Screen matches site default** light/dark-aware styling (not a separate dark theme).

## Routes & access

- `/live` — big screen. `[Authorize(Roles="SiteAdmin")]`. QR, animated pipeline, live count,
  live feed, and a **Reset** control (clears the live count/feed between talks; saved leads
  are untouched).
- `/live/join` — public phone form (name + email only — minimal for a queue of strangers).
- `/api/live/qr.svg` — minimal-API endpoint; renders the QR for the absolute `/live/join`
  URL (built from `HttpContext` request scheme/host; honours a `PUBLIC_BASE_URL` config
  override if present).

## Components & files

### New — `FellsideDigital.Web`

- `Services/LiveShowcaseState.cs` — singleton, thread-safe in-memory broadcaster.
  - `LiveParticipant` record: `Name`, `Company?`, `JoinedAt`.
  - `void Publish(LiveParticipant)` — appends to recent (cap 8), increments count, raises `ParticipantJoined`.
  - `LiveSnapshot Snapshot()` — `{ int Count, IReadOnlyList<LiveParticipant> Recent }` for a screen joining mid-event.
  - `void Reset()` — clears count + recent, raises a `Reset` event.
  - `event Action<LiveParticipant>? ParticipantJoined;` and `event Action? ResetRequested;`
  - Registered as a singleton in `ServiceConfigurationExtensions`.
- `Services/CompanyResolver.cs` — ported as a **static** helper: derive a display company
  from an email domain (skips generic domains, handles multi-part TLDs like `co.uk`). Returns `string?`.
- `Services/EmailValidator.cs` — ported as a **static** helper: lightweight structural email validation.
- `Components/Pages/Live/Screen.razor` (+ `.razor.cs`) — the big screen. Subscribes to
  `LiveShowcaseState` on init, `IDisposable` unsubscribes; a per-participant animation queue
  (mirrors the standalone `drain()`) plays one pipeline at a time; `InvokeAsync(StateHasChanged)`
  marshals updates onto the circuit. Renders count-up, five-stage pipeline, live feed, Reset.
- `Components/Pages/Live/Join.razor` (+ `.razor.cs`) — public phone form. On submit:
  validate → resolve company → persist lead → publish to `LiveShowcaseState` → send email
  (non-fatal). Uses `FieldStyles.MarketingInput` and the `accent` button styling like `/scan`.
- `Endpoints/LiveQrEndpoint.cs` (or inline in a `Map…` extension) — `/api/live/qr.svg`.

### New — email

- `Services/Email/EmailTemplates.cs` → add `LiveAutomationWelcome(QrLead lead)` using
  `EmailTheme.Layout`, `H2`, `P`, `EmailTheme.Button`. Subject constant lives with the send method.
- `Services/IEmailService.cs` + `Services/EmailService.cs` → add
  `Task SendLiveAutomationWelcomeAsync(QrLead lead)` calling the private `SendAsync(...)`.

### Changed

- `ServiceConfigurationExtensions.cs` — register `LiveShowcaseState` as a singleton.
  (`CompanyResolver` / `EmailValidator` are static — no registration.)
- `FellsideDigital.Web.csproj` — add `<PackageReference Include="QRCoder" .../>`.
- Program pipeline / an endpoint-mapping extension — map `/api/live/qr.svg`.
- `_Imports.razor` — add `@using` for a new `Components.Pages.Live` namespace only if needed.

### Not changed

- No DB migration — `QrLead` already has every field required. `QrLeadService.CreateLeadAsync`
  accepts any `Source`, so `"live"` works. `GetLeadsAsync` already returns all leads, so live
  leads appear in the admin dashboard with no dashboard change.

## Real-time data flow

```
Phone /live/join submit
  → EmailValidator.IsValid(email)                          (inline error if invalid)
  → CompanyResolver.Resolve(email)                         → company?
  → QrLeadService.CreateLeadAsync(new QrLead {
        Source="live", Name, Email, Company, Interest="Automation" })   [persist]
  → LiveShowcaseState.Publish(new LiveParticipant(Name, Company, now))  [in-memory + event]
  → EmailService.SendLiveAutomationWelcomeAsync(lead)      [try/catch, non-fatal, LogAndDescribe]
  → phone shows success ("look up at the screen")

/live screen circuit (subscribed to LiveShowcaseState.ParticipantJoined)
  → enqueue participant → animate pipeline stage-by-stage → count-up → prepend feed item
```

The five pipeline stages mirror real server-side actions (lead captured, enriched via
`CompanyResolver`, persisted, welcome email sent, done) — the demo is honest, not faked.

## Lead field mapping (source = "live")

| Field | Value |
|---|---|
| `Source` | `"live"` |
| `Name` / `Email` | from form |
| `Company` | `CompanyResolver.Resolve(email)` (nullable) |
| `Interest` | `"Automation"` (it is an automation demo; keeps the entity's required field meaningful) |
| `Phone`, `Budget`, `Timeline`, `Message`, `QrScanId` | `null` |

## Error handling & security (per site rules)

- Big screen `[Authorize(Roles="SiteAdmin")]`; join page anonymous (site has no global auth
  fallback — public pages like `/scan` confirm this).
- All risky operations in `try/catch` → `ErrorHandling.LogAndDescribe`; email send is
  non-fatal (lead is already saved). No `ex.Message` shown to users.
- No secrets in code; email uses the existing configured Graph pipeline. No raw SQL (EF Core only).
- The QR endpoint only reflects the request host / configured base URL into a QR — no user input echoed into HTML.

## "Improve it as it moves" (baked in)

- Blazor-native, no CDN Tailwind / CDN SignalR — brand-consistent via design tokens, light/dark aware.
- Animated count-up, per-stage glow + tick reveal, completion pulse.
- Snapshot replay so a screen opened mid-event shows the current count + recent feed.
- Circuit auto-reconnect (built into Blazor Server).
- Admin **Reset** control to zero the live count/feed between talks without losing leads.
- Live feed enriched with resolved company names.

## Testing

- `CompanyResolver` — generic domains, `co.uk`/`ac.uk`, empty/malformed input (pure).
- `EmailValidator` — valid/invalid structures (pure).
- `LiveShowcaseState` — publish increments count + raises event; recent capped at 8;
  snapshot reflects state; reset clears + raises (pure, no fixture).
- `EmailTemplates.LiveAutomationWelcome` — renders, contains the name and offer CTA (pure).
- `QrLeadService` — a `"live"` lead persists and is returned by `GetLeadsAsync`
  (Postgres Testcontainers fixture; Docker required).

## Out of scope

- Changes to the existing `/scan` flow, admin dashboard stats, or `QrScan` recording for live.
- Multi-session/event grouping — a single global live session; count is in-memory and resets
  on app restart or via the Reset control. Leads persist regardless.
- Retiring the standalone `FellsideDigital/Presentation` app (left as-is).
