# Testimonial request — copy link + completion email

**Date:** 2026-06-18
**Status:** Approved (design)

## Problem

After a project wraps, the admin wants two low-friction ways to prompt a client
for a testimonial:

1. Copy a link to the testimonial form so it can be pasted into a message and
   sent to the client directly.
2. Once a project is **Completed**, send the client a branded email asking them
   to leave a testimonial.

Both should point the client at the existing testimonial form. There is no
need for a separate "update" feature — the portal form already supports
editing (see *Out of scope*).

## Context (existing behaviour)

- The testimonial form lives at `/Portal/Testimonial`, gated by `[Authorize]`.
  It is the **same URL for every client**; clients reach it from inside their
  portal after logging in.
- `Testimonial.razor.cs` already pre-fills a client's existing rating, quote,
  name and company, switches the button to "Update testimonial", and
  `ITestimonialService.SubmitOrUpdateAsync` handles both create and edit
  (re-setting status to `Pending` for re-review). **Editing already works.**
- Real email is sent via Microsoft Graph in `EmailService`. Client-facing
  emails (e.g. `SendInvoiceAddedAsync`) BCC the admin and build an absolute
  portal URL via `NavigationManager.ToAbsoluteUri(...)`. Email templates live
  in `EmailTemplates` (pure static class) and share theming via `EmailTheme`.
- Clipboard copy is an established pattern:
  `JS.InvokeVoidAsync("navigator.clipboard.writeText", url)` followed by a
  success toast (`Admin/Invitations/Index.razor.cs:43`).
- The admin **project Detail** page (`Admin/Projects/Detail.razor`) shows a
  Client card with an "Email client" `mailto:` link, and the project's
  `Status`. `_project.ClientId` is the client's `ApplicationUser` id.

## Decisions

- **Link type:** login-required portal link (`/Portal/Testimonial`). No
  tokenised/public link. Clients already have portal accounts.
- **Copy button placement:** project Detail page only (Client card).
- **Email availability:** the "Request a testimonial" button appears **only when
  `_project.Status == ProjectStatus.Completed`** and the client has **not**
  already submitted a testimonial. If they have, show a small note instead of
  the button.

## Design

### 1. Copy testimonial link

- Add a **"Copy testimonial link"** button to the Client card in
  `Admin/Projects/Detail.razor`, beside the existing "Email client" link.
- `Detail.razor.cs` gains `IJSRuntime` and a `CopyTestimonialLinkAsync()` method
  that copies `NavigationManager.ToAbsoluteUri("/Portal/Testimonial").ToString()`
  to the clipboard and raises `Toasts.Success(...)`, wrapped in `try/catch` →
  `ErrorHandling.LogAndDescribe` per project convention.
- Always available, independent of project status.

### 2. "Request a testimonial" completion email

- Add a **"Request a testimonial"** button to the Detail page **Client card**
  (below the existing "Email client" link, beside "Copy testimonial link"),
  rendered only when the project is `Completed` **and**
  `_clientHasTestimonial == false`. When the client already has one, render a
  muted note: "This client has already left a testimonial."
- New `EmailService.SendTestimonialRequestAsync(ApplicationUser client,
  ClientProject project, string testimonialUrl)` — sends to the client with
  `bccAdmin: true`, mirroring `SendInvoiceAddedAsync`.
- New `EmailTemplates.TestimonialRequest(ApplicationUser client,
  ClientProject project, string url)` — branded HTML with a CTA button linking
  to the testimonial form, following the existing template/theme style.
- `Detail.razor.cs`:
  - Inject `ITestimonialService`.
  - In `LoadAsync`, set `_clientHasTestimonial =
    await Testimonials.GetForUserAsync(_project.ClientId) is not null`.
  - `RequestTestimonialAsync()` builds the absolute URL, calls
    `SendTestimonialRequestAsync`, and toasts the outcome. Email failure surfaces
    a friendly `Toasts.Error(...)` via `ErrorHandling.LogAndDescribe`; it never
    blocks the page. A `_sendingRequest` flag drives a `SpinnerButton`.

### 3. Error handling & feedback

Per `CLAUDE.md`: every action wrapped in `try/catch`; never surface
`ex.Message`; log via `ErrorHandling.LogAndDescribe(Logger, ex, "…")`; report
outcomes with `ToastService`.

### 4. Testing

- `EmailTemplates` is a pure static class. Add a unit test (no fixture) asserting
  `TestimonialRequest(...)` output contains the testimonial URL and the client's
  name. Email **delivery** (Graph) stays untested, consistent with the other
  email methods.

## Out of scope (YAGNI)

- Tokenised / public (no-login) links.
- Auto-send on completion or scheduled reminders.
- A DB field tracking "requested".
- A separate "update testimonial" feature — the portal form already edits.
- Admin-side editing of a client's testimonial text.
