# Client Testimonials — Design

**Date:** 2026-06-11

## Goal

Let existing clients submit a testimonial (1–5 star rating + a quote) from a
logged-in page. Submissions are held for admin approval, and approved
testimonials drive the public home-page testimonials section (replacing the
hardcoded static list).

## Decisions (from brainstorming)

- **Moderation:** approve-first. Submissions land `Pending`; only `Approved`
  ones appear publicly.
- **Eligibility:** any logged-in user reaching `/portal/testimonial`. Hitting it
  logged-out bounces to login via `[Authorize]`.
- **Stars:** shown on each public card (real rating, not a fixed 5).
- **Attribution:** name + role/company. Captured by adding a **`JobTitle`**
  field to the client model and the admin invite flow; the rest
  (`FirstName`/`LastName`/`CompanyName`) already exist. On the submission form
  Name/Role are prefilled from the account but **editable** (covers existing
  clients with no `JobTitle`).
- **Re-submission:** one testimonial per client. Returning clients edit their
  existing one; editing resets it to `Pending`.

## Data model

New `JobTitle` field:
- `ApplicationUser.JobTitle` (string?)
- `ClientInvitation.JobTitle` (string)
- Added to the admin invite Create form + carried through `Register.razor` onto
  the new user (mirrors how `CompanyName` flows today).

New entity `Data/ClientTestimonial`:
- `Id` (Guid)
- `UserId` (string, FK → ApplicationUser) — unique, one per client
- `Rating` (int 1–5)
- `Quote` (string)
- `AuthorName`, `AuthorRole` — snapshot at submit time so public cards stay
  stable if the account later changes
- `Status` — `TestimonialStatus { Pending, Approved, Rejected }`
- `SubmittedAt`, `UpdatedAt`, `ApprovedAt?`

## Service — `ITestimonialService` / `TestimonialService`

Registered in `ServiceConfigurationExtensions.ConfigurePortalServices`.

- `GetApprovedAsync()` — approved only, newest first (public page).
- `GetForUserAsync(userId)` — existing submission or null (prefill/edit).
- `SubmitOrUpdateAsync(userId, rating, quote, authorName, authorRole)` — upsert;
  sets `Pending`; validates rating 1–5 and non-empty quote, throwing
  `InvalidOperationException` with a user-facing message.
- `GetAllAsync()` — all statuses (admin).
- `SetStatusAsync(id, status)` — approve/reject (sets `ApprovedAt`).
- `DeleteAsync(id)`.

## Pages

- **Client submission** — `/portal/testimonial`, `[Authorize]`, `PortalLayout`,
  sidebar link. Heading "Thank you for taking the time to write us a
  testimonial." Interactive 1–5 star picker, quote textarea, editable Name/Role
  prefilled from the account. Returning clients see their submission prefilled.
  Success → toast + "pending review" note.
- **Admin moderation** — `/Admin/Testimonials`,
  `[Authorize(Roles="SiteAdmin")]`, `AdminLayout`, sidebar link. Table of all
  testimonials (status badge, stars, quote, author) with Approve / Reject /
  Delete.
- **Public home** — `Home.razor` injects the service, loads approved in
  `OnInitializedAsync`. Fallback: if none approved, render the existing static
  `HomeData.Testimonials` so the section is never empty.

## UI

- `TestimonialCard` gains a `Rating` parameter (default 5) → filled vs empty
  stars.
- Inline star picker on the submission form (buttons, Interactive Server).
- Feedback via `ToastService` / `AlertBanner`; errors via
  `ErrorHandling.LogAndDescribe` per project conventions.

## Migration & tests

- One EF migration: `JobTitle` columns on users + invitations, and the
  `ClientTestimonials` table.
- DB-backed `TestimonialService` tests (Testcontainers): upsert / one-per-user,
  edit-resets-to-pending, approve flow, `GetApprovedAsync` filtering.
