# Dynamic Quote Estimator (`/quote`)

**Date:** 2026-07-08
**Status:** Approved design — ready for implementation plan

## Goal

Give prospective clients a fast, self-serve way to get a **ballpark price** for their
project and turn that into a warm enquiry. Instead of the heavy 3-step `/contact`
wizard, a new `/quote` page asks a few tap-to-answer questions, shows a **live
estimated price range**, then captures their details and drops the request into the
same enquiry inbox we already have.

## Why this shape

- We already sell fixed(ish) starting prices on `/websites` and `/automation`, so the
  raw numbers to drive an estimate already exist in the codebase.
- `/scan` (the QR campaign) already implements the exact interaction we want —
  tap-to-select option buttons, validation, save-a-lead + notification email, and a
  polished success panel. We reuse that pattern rather than reinventing it.
- The contact/enquiry backend (`ContactEnquiry` + `IEnquiryService` +
  `EmailService.SendContactEnquiryAsync`) already persists and notifies. All columns
  are `text` (no length caps), so a rich, auto-composed message fits with **no schema
  change**.

## Decisions (locked)

| Decision | Choice |
|---|---|
| Relationship to `/contact` | New `/quote` page; `/contact` stays untouched. |
| Estimate presentation | **A range** (e.g. "Estimated £795 – £1,100 one-off"), never a hard quote. |
| Scope | **Websites + automation.** |
| Backend | Reuse `ContactEnquiry` → `IEnquiryService.CreateAsync` → `EmailService.SendContactEnquiryAsync`. No new table, no migration. |

## Non-goals (YAGNI)

- No new database table or migration.
- No admin UI changes — estimator enquiries appear in the existing enquiries list;
  their selections/estimate ride along inside the `Message` field.
- No integration with the `/scan` `LAUNCH26` discount code (possible future tie-in,
  not built now).
- No user accounts, no saved/emailed PDF quote. The estimate is shown on screen and
  embedded in the enquiry we receive.

---

## Page & route

- `Components/Pages/Marketing/Quote.razor` + `Quote.razor.cs`, route `@page "/quote"`.
- **Interactive Server** render mode (site default), `MainLayout`.
- Single screen, marketing styling: reuse `FieldStyles.MarketingInput` and the same
  hero / section look as `/contact` and `/scan`. Reuse the `ToggleClass` selected/
  unselected button styling from `Scan.razor.cs`.
- Below the form, include the compact **call / WhatsApp** options (for people who'd
  rather just talk) — mirrors `/contact`'s reassurance.

## Estimator UX (single screen, live-updating)

The estimate line updates immediately as the user taps options.

**Section A — Website** (optional; toggled on by "I need a website"):
1. **What kind of site?** (single-select → base price)
   - "A few pages about my business" → Brochure (£295)
   - "Bookings, integrations, dynamic content" → Business (£495)
   - "Custom system / web-app" → Advanced (£1,750)
2. **Add-ons** (multi-select, each adds to the total) — see pricing table.
3. **Ongoing care?** (single-select) — None / Basic / Standard / Premium (shown as a
   separate £/mo line).

**Section B — Automation** (optional; toggled on by "I need automation"):
1. **Scale?** (single-select → base price)
   - "Automate a few repetitive tasks" → Small (£150)
   - "Connect multiple tools / teams" → Mid (£400)
   - "Enterprise-grade, audited, at scale" → Enterprise (£900)
2. **Add ongoing support?** (yes/no) — adds a "from £X/mo" note to the monthly line.

**Live estimate panel:**
- One-off: **range** = `subtotal` → `roundUp(subtotal × upliftFactor)`.
- Monthly: care plan (point £/mo) + automation support (adds "from" framing).
- Disclaimer: *"An indicative estimate — your final fixed price is confirmed after a
  quick chat."*

**Lead capture (revealed / enabled once at least one base option is chosen):**
- Name *(required)*, Email *(required)*, Phone *(optional)*, "Anything else?"
  *(optional textarea)*.
- Submit button disabled until name + email + at least one base selection are present.

## Pricing model — **numbers to confirm**

Seeded from existing site data. The **base prices and care/automation prices are
already live on the site**; the **add-on increments and uplift factor are new and are
Oliver's to confirm** before/at spec review.

**Website base** (from `/websites`): Brochure £295 · Business £495 · Advanced £1,750.

**Website add-ons** (NEW — confirm):
| Add-on | Increment |
|---|---|
| E-commerce / online store | +£600 |
| Booking & scheduling | +£300 |
| Copywriting | +£250 |
| Extra pages (bundle) | +£150 |
| Branding / logo | +£300 |

**Care plans** (from `/websites`, £/mo): Basic £10 · Standard £20 · Premium £40.

**Automation base** (from `/automation`): Small £150 · Mid £400 · Enterprise £900.
Automation support: optional monthly, presented as "from £10/mo" framing (existing
copy quotes £10–£150/mo depending on scale).

**Range uplift factor** (NEW — confirm): `high = roundUpTo50(subtotal × 1.40)`.

## Logic lives in a service (per project conventions)

Pricing/estimation is business logic, so it goes in a **pure, DB-free, unit-testable**
service — not in the component.

- `Services/IQuoteEstimatorService.cs` / `QuoteEstimatorService.cs`
  - `QuoteEstimate Estimate(QuoteSelection selection)`
- Register in `ServiceConfigurationExtensions` alongside the other portal/marketing
  services.
- Price rules held as plain `static readonly` data/records inside the service (or a
  small `QuotePricing` record set) so the numbers live in one obvious place.

**Types (domain-ish, framework-free where practical):**

```
enum WebsiteType     { Brochure, Business, Advanced }
enum WebsiteAddOn    { Ecommerce, Booking, Copywriting, ExtraPages, Branding }
enum CarePlanLevel   { None, Basic, Standard, Premium }
enum AutomationScale { Small, Mid, Enterprise }

record QuoteSelection(
    bool NeedsWebsite, WebsiteType? WebsiteType, IReadOnlySet<WebsiteAddOn> AddOns,
    CarePlanLevel Care,
    bool NeedsAutomation, AutomationScale? AutomationScale, bool AutomationSupport);

record QuoteEstimate(
    decimal OneOffLow, decimal OneOffHigh,
    decimal MonthlyFrom,       // care + support entry point; 0 => none
    bool HasEstimate);         // false when no base option chosen
```

Rounding: round `OneOffHigh` up to nearest £50. `HasEstimate` is false when neither a
website type nor an automation scale is selected.

## Submission — reuse the existing enquiry pipeline

On submit, `Quote.razor.cs` composes a `ContactEnquiry` and calls the same services
`/contact` uses. Same `try/catch` + `ToastService` + `ErrorHandling.LogAndDescribe`
pattern; save is authoritative, notification email failure is non-fatal (logged).

Field mapping:
- `Name`, `Email`, `Phone` — from the capture fields.
- `ServiceType` — summary string: `"Website"`, `"Automation"`, or `"Website + Automation"`.
- `Budget` — the estimated range string, e.g. `"Est. £895–£1,300 (estimator)"`.
- `HowHeard` — `"Quote estimator"` (so these are distinguishable in the inbox).
- `Message` — auto-composed breakdown, then the user's free-text note. Example:

```
--- Quote estimator ---
Website: Business site (£495)
  Add-ons: E-commerce, Copywriting
  Care plan: Standard (£20/mo)
Automation: Small Business (£150) + ongoing support
Estimated one-off: £895 – £1,300
Estimated monthly: from £30/mo

Their note:
<free text, or "—">
```

On success, swap the form for a success panel (like `/scan`): "Thanks — I'll come back
within one working day with your detailed quote," echoing their estimate.

## CTA rewiring

Repoint the buttons Oliver named, all on `/websites` (`Websites.razor`):
- Line ~204 **"Get a quote"** (pricing tier cards) → `/quote`
- Line ~263 **"Build my package"** (flagship bundle) → `/quote`
- Line ~470 **"Get a quote"** (bottom CTA) → `/quote`

Out of scope (unchanged): the `/websites` hero **"Start your project"** button and all
nav/footer contact links keep pointing at `/contact`. (Trivial to also switch "Start
your project" later if wanted.)

## SEO

- `SeoHead` with title/description/canonical for `/quote`.
- `ContactPage` + `BreadcrumbList` JSON-LD, mirroring `/contact`.
- Add `/quote` to the sitemap if one is maintained in-repo (check during
  implementation; add only if a static/generated sitemap exists).

## Error handling & conventions

- No `ex.Message` to users; wrap the save in `try/catch` →
  `ErrorHandling.LogAndDescribe(Logger, ex, "sending your quote request")`, show via
  `ToastService`.
- Inject `ILogger<Quote>`. Component orchestrates only; all pricing math is in
  `QuoteEstimatorService`.
- No secrets, no raw SQL, no auth changes (public marketing page — no `[Authorize]`).

## Testing

- `tests/FellsideDigital.Tests` — **pure-logic unit tests** for `QuoteEstimatorService`
  (no Postgres fixture needed):
  - website-only, automation-only, and combined subtotals;
  - each add-on and care level contributes correctly;
  - range = subtotal → roundUpTo50(subtotal × factor);
  - `HasEstimate` false when no base option selected;
  - monthly-from reflects care + support.

## Files touched

**New**
- `Components/Pages/Marketing/Quote.razor`
- `Components/Pages/Marketing/Quote.razor.cs`
- `Services/IQuoteEstimatorService.cs`, `Services/QuoteEstimatorService.cs`
- (enums/records — either in the service file or a small `Quote` model file)
- `tests/FellsideDigital.Tests/QuoteEstimatorServiceTests.cs`

**Edited**
- `Components/Pages/Marketing/Websites.razor` — 3 CTA hrefs → `/quote`
- `Extensions/ServiceConfigurationExtensions.cs` — register `IQuoteEstimatorService`

## Open items to confirm at review

1. Website **add-on increments** (table above) — real numbers?
2. **Uplift factor** for the high end of the range (1.40 seeded)?
3. Automation **support monthly** framing ("from £10/mo") — OK as a note, or omit?
