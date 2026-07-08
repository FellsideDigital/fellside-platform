# Websites page: real project showcase + pricing move

**Date:** 2026-07-08
**Page affected:** `src/FellsideDigital.Web/Components/Pages/Marketing/Websites.razor` (`/websites`)

## Goal

Replace the hardcoded placeholder projects in the "What we've built" (Portfolio)
section with the real, hero-flagged client projects, presented as alternating
feature rows. Move the Pricing section up to sit directly under the Portfolio
section. Purely presentational — no DB migration, no service or business-logic
changes.

## Background

- The "What we've built" section on `/websites` currently renders four hardcoded
  placeholders from `HomeData.Projects` (`Home.razor.model.cs`) with fake
  browser-mockup graphics.
- The app already has a database-driven showcase system. `ClientProject` carries
  `Name`, `HeroTagline`, `Description`, `Type` (Website/Automation),
  `ScreenshotPath`, `HeroShowcaseUrl`, `ProjectUrl`, plus `Metrics`,
  `Integrations`, and `PipelineSteps`.
- `IHeroProjectService.GetHeroProjectsAsync()` already returns the curated,
  hero-flagged projects (`IsHeroProject`), ordered by `HeroDisplayOrder`, with
  screenshots resolved to displayable URLs and metrics/integrations/pipeline
  steps eager-loaded. The homepage hero carousel consumes exactly this.

## Decisions

1. **Data source:** the Portfolio section injects `IHeroProjectService` and calls
   `GetHeroProjectsAsync()` — the same curated set as the homepage carousel.
   Opting a project into the public showcase is done in the admin (the existing
   `IsHeroProject` flag); it then appears in both the carousel and this grid.
   Chosen because `IsHeroProject` is the explicit "safe to show publicly" signal,
   requires no migration/admin work, and keeps the two showcases in sync.
2. **Presentation:** alternating full-width feature rows (not the compact cards),
   reusing the `reverse = index % 2 != 0` pattern already used by the "Website
   Services" section directly below.
3. **Pricing:** moved to immediately follow the Portfolio section.

## Design

### Portfolio section (rewired)

- Inject `@inject IHeroProjectService HeroProjectService` on `Websites.razor`
  and load projects in `OnInitializedAsync` into a `List<ClientProject>` field,
  **filtered to `ProjectType.Website`** — this is the website-design page, so
  automation projects are excluded (they belong on the automation page).
- If the list is empty, render the shared `EmptyState`
  (`UI.Components.Feedback`) — "No featured projects yet" — matching the
  homepage testimonials empty-state pattern. Otherwise render one
  `ProjectShowcaseRow` per project, passing the project and its index.

### New component: `ProjectShowcaseRow.razor`

Location: `src/FellsideDigital.Web/Components/Pages/Marketing/ProjectShowcaseRow.razor`

The component is **website-only** (this page shows only website projects).

Parameters:
- `ClientProject Project` (EditorRequired)
- `int Index` (EditorRequired) — drives the alternating side via `Index % 2 != 0`.

Layout: `grid md:grid-cols-2 gap-12 py-16 items-center`, with the visual panel
given `md:order-last` on odd rows.

**Visual panel:** browser-chrome frame (traffic-light dots + URL bar) containing a
**live iframe** of `Project.PreviewUrl`, exactly as the hero carousel does —
reusing the shared `heroCarousel.tryLoadIframe` / `onIframeLoad` JS (loaded
globally in `App.razor`). The iframe starts hidden behind a fallback
(`Project.ScreenshotPath` `<img>`, or a wireframe placeholder) and swaps in once
it loads; if the site refuses framing (X-Frame-Options/CSP) the fallback stays.
Each row uses project-id-keyed iframe/fallback element ids so multiple live
previews coexist on the page.

**Content panel:** name, tagline, up to three metric pills, tech tags from
`Integrations`, and a "View site →" link. No type badge — every row is a website,
so a "Website" badge would be redundant.

Both the page and this component render `InteractiveServer` (global render mode
in `App.razor`), so `OnAfterRenderAsync` JS interop runs.

**Content panel:**
- Type badge (🌐 Website / ⚡ Automation) with the same colour treatment as the
  carousel's `TypeBadgeClasses`.
- Name (`Project.Name`).
- Tagline: `Project.HeroTagline` when present, else `Project.Description`.
- Up to three metric pills from `Project.Metrics` (ordered by `DisplayOrder`),
  rendered only when present.
- Tech tags from `Project.Integrations` names.
- "View site →" link to `Project.HeroShowcaseUrl ?? Project.ProjectUrl`, rendered
  only when one is set (external link: `target="_blank" rel="noopener noreferrer"`).

**Tradeoff:** the component keeps its own small copies of the badge / metric /
pipeline styling helpers rather than sharing them with `HeroProjectCarousel`.
Extracting shared helpers would require editing the working carousel — out of
scope and higher-risk for a presentational change. Blast radius is kept to
"add a component, edit one page."

### Section reorder + background re-striping

New section order on `/websites`:

`Hero → Portfolio → Pricing → Capabilities(bento) → Website services → Areas → CTA`

To preserve the slate-50 / white zebra striping, adjust backgrounds:

| Section (new order) | Background |
|---|---|
| Portfolio | `bg-slate-50 dark:bg-neutral-900` (unchanged) |
| Pricing | `bg-white dark:bg-neutral-950` (unchanged class, moved) |
| Capabilities (bento) | `bg-slate-50 dark:bg-neutral-900` (was white) |
| Website services | `bg-white dark:bg-neutral-950` (was slate-50) |
| Areas we serve | `bg-slate-50 dark:bg-neutral-900` (unchanged) |
| CTA | `bg-white dark:bg-neutral-950` (unchanged) |

The `#pricing` anchor id is retained, so the hero's "See pricing" button still
works. No content inside any moved section changes.

### Cleanup

`HomeData.Projects` and the `Project` record in `Home.razor.model.cs` become
dead (only referenced by this section) and are removed.

## Out of scope / non-goals

- No changes to `HeroProjectService`, `ProjectService`, or any query.
- No DB migration; no changes to the admin.
- No changes to the homepage or any other page.

## Verification

- `dotnet build` succeeds (Tailwind regenerates via MSBuild target).
- Drive `/websites`: Portfolio shows real hero projects as alternating rows
  (or the empty state when none), Pricing sits directly beneath it, and the
  slate/white striping reads cleanly in both light and dark themes.

## Testing

Presentational only — no service/business-logic change, so no new automated
tests per the project's testing convention. Verified by build + driving the page.
