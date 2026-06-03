# Refactoring Audit & Prioritised Backlog

**Date:** 2026-06-03
**Scope:** Whole-codebase audit against the project's refactoring & architecture standards.
**Status:** Audit complete — awaiting selection of first implementation slice.

This document is the output of a **read-only** audit. Nothing has been changed. It
turns the broad standards document into a concrete, prioritised list of slices, each
of which will get its own spec → plan → implementation cycle.

---

## 1. Current state (the good news)

The codebase is **already partway through a deliberate cleanup** and is in decent shape:

- Clean project separation: `FellsideDigital.Domain`, `FellsideDigital.UI`, `FellsideDigital.Web`.
- A real `.UI` component library (~20 components: buttons, cards, feedback, icons, navigation).
- A service layer with interfaces (`IProjectService`, `IInvoiceService`, `IInvitationService`,
  `IHeroProjectService`, `IStorageService`) — most pages correctly depend on these, not the DB.
- Code-behind pattern (`.razor` + `.razor.cs`) used consistently.
- Startup composition already extracted into `Extensions/`.

So this is **refinement, not rescue.** The work below sharpens consistency and kills
duplication; it is not a rewrite.

---

## 2. Findings (evidence-based)

### F1 — UI form primitives are duplicated everywhere *(highest-value DRY win)*
The same input styling string —
`block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 ... focus:ring-accent` —
is copy-pasted across **15+ files**, sometimes as a re-declared local `InputClass` const
(`Edit.razor.cs:49`, `Create.razor.cs`, `Invoices.razor.cs`, `Settings.razor.cs`, …),
sometimes inline in markup. There is **no** `Input`/`Select`/`Textarea`/`Field` component
in `.UI`. Raw `<button>` appears in **39 places**, raw `<table>` in **6**, ad-hoc status
badges in **28**, inline empty-state markup in **28** — despite `.UI` already having
`PrimaryButton`, `SpinnerButton`, `StatusBadge`, and `EmptyState`.

### F2 — A few pages bypass the service layer *(separation of concerns)*
Four pages inject `FellsideDigitalDbContext` directly and call `.Add()/.SaveChangesAsync()`:
- `Pages/Admin/Enquiries/Index.razor.cs`
- `Pages/Admin/QrCampaign/Index.razor.cs`
- `Pages/Marketing/Contact.razor.cs`
- `Pages/Marketing/Scan.razor.cs`

There is no `IEnquiryService` or `IQrLeadService`. (Note: `PortalLayout.razor.cs` only
*mentions* DbContext in a comment about circuit-scoped contexts — it is not a bypass.)

### F3 — Inconsistent, leaky error handling
Error handling is ad-hoc: repeated `catch (Exception ex) { _errorMessage = $"Failed to
save: {ex.Message}"; }` (5+ sites). This **leaks raw exception text to the UI** and there
is no shared result/notification convention. No standard toast/alert flow on success.

### F4 — `Edit.razor` is too large and does too much
`Edit.razor` (616 lines) + `Edit.razor.cs` (370 lines) carries 5 editor sub-models and
four near-identical "reorderable list" editors (phases, metrics, pipeline steps,
integrations) each with hand-written Add/Remove/Move logic, plus repeated entity↔model
mapping in `OnInitializedAsync`/`SaveAsync`. Candidate for a generic reorderable-list
editor component and extracted mapping.

### F5 — Manual entity↔view-model mapping duplicated
Hand mapping between entities and editor models is repeated across admin create/edit
pages. Candidate for mapping extension methods (kept simple — no need for a mapper library).

### F6 — Dead code & stale docs
- Stale top-level `FellsideDigital/` directory — only a `.csproj.user` + build artifacts
  remain; the live code is under `src/`.
- `Pages/Marketing/AnimationsExample.razor` — a demo page, not referenced anywhere.
- `CLAUDE.md` is **out of date**: it points at `FellsideDigital/` (should be `src/...`),
  the dev port (`:5185` vs the `:8080` actually used), and migration commands targeting
  the wrong project.

### F7 — No test project exists at all
The standards require "improve test coverage where modifications occur," but there is no
test harness in the solution. Coverage can't be added to a slice until one exists.

### F8 — Design patterns: apply sparingly
Little in the current code genuinely calls for Strategy/Command/CQRS. The honest
candidates are narrow: badge/status → label/colour mapping (Strategy-ish, already
partly in `BadgeHelpers`), and the reorderable-list editors (F4). **Recommendation:
do not introduce pattern ceremony for its own sake** — the standards say "apply
pragmatically," and the real wins here are F1–F3.

---

## 3. Prioritised backlog

Ranked by value ÷ effort and by what unblocks later work.

| ID | Slice | Addresses | Effort | Risk | Value |
|----|-------|-----------|--------|------|-------|
| **S1** | **`.UI` form primitives** (`FormField`, `TextInput`, `SelectInput`, `TextArea`) + adopt across admin/account/portal forms | F1 | M | Low | ★★★ |
| **S2** | **Extract `IEnquiryService` + `IQrLeadService`**; remove `DbContext` from the 4 pages | F2 | S | Low | ★★★ |
| **S3** | **Dead-code & docs cleanup**: delete stale `FellsideDigital/` dir + `AnimationsExample`; fix `CLAUDE.md` | F6 | S | Low | ★★ |
| **S4** | **Adopt existing `StatusBadge` + `EmptyState`** everywhere they're hand-rolled | F1 | S | Low | ★★ |
| **S5** | **`.UI` `Table` component**; adopt across the 6 table pages | F1 | M | Low | ★★ |
| **S6** | **Standard error-handling + toast/alert convention** (Result type + shared notifier; stop leaking `ex.Message`) | F3 | M | Med | ★★ |
| **S7** | **Add an xUnit test project**; cover services touched by S2/S6 | F7 | M | Low | ★★ |
| **S8** | **Decompose `Edit.razor`**: generic reorderable-list editor + extracted models/mapping | F4, F5 | L | Med | ★ |
| **S9** | **`.UI` `Modal`** (consolidate `ConfirmModal` + `ReconnectModal`) | F1 | S | Low | ★ |

Effort: S ≈ <½ day, M ≈ ~1 day, L ≈ multi-day.

---

## 4. Recommended sequence

1. **S3** first — cheap, de-risks everything, makes the repo honest (and fixes `CLAUDE.md`
   so all later work is grounded in correct commands/paths).
2. **S2** — small, clean separation-of-concerns win; gives the test project (S7) something
   meaningful to cover.
3. **S1** — the biggest single duplication win and most visible; aligns directly with the
   "UI component library first" principle.
4. Then S4 → S5 → S6 → S7 → S8 → S9 as appetite allows.

Each slice preserves existing functionality, is independently reviewable, and adds tests
where it touches logic (once S7 lands).

---

## 5. Explicitly out of scope (YAGNI)

- No CQRS/MediatR, no mapper libraries, no Strategy/Command scaffolding unless a specific
  slice proves it earns its keep.
- No rewrite of the marketing pages' static markup beyond swapping in shared primitives.
- No change to the rendering model, auth, or data schema.
