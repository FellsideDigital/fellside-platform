# Email redesign + document/invoice notifications

**Date:** 2026-06-03
**Status:** Approved

## Goal

1. Re-skin all transactional emails from orange to the blue brand palette.
2. Show the real Fellside logo in the email header (reliably across clients).
3. Centralise the email design so colour/branding lives in one place.
4. Notify the client (with an admin copy) whenever a new document or invoice
   is added to their project, and when an invoice moves to Sent/Overdue.

## Decisions

- **Logo:** inline SVG does not render in Gmail/Outlook, and hosted images get
  hidden by "block remote images". Use a **CID-embedded inline attachment** of
  the existing blue PNG logo (`wwwroot/web-app-manifest-512x512.png`), referenced
  as `<img src="cid:fellside-logo">`. Always renders, no remote fetch.
- **Colours:** accents/borders/highlights → blue-400 `#60a5fa`; button fills →
  blue-600 `#2563eb` with white text (passes AA contrast). Replaces orange
  `#fb923c`/`#f97316` and indigo `#6366f1` link/CTA colours.
- **Notifications:** client is the recipient; admin (`EmailSettings.AdminEmail`)
  is BCC'd as a silent receipt. Triggers: document upload, invoice upload, and
  invoice status → `Sent`/`Overdue` (never `Draft`/`Paid`).
- **Failure isolation:** notification sends are wrapped in try/catch + `ILogger`;
  a mail failure never breaks an upload or status change.

## Structure (isolation)

- `Services/Email/EmailTheme.cs` — colour tokens + shared building blocks
  (`Layout(...)`, `Button(...)`, `InfoTable(...)`). Single source of brand truth.
- `Services/Email/EmailTemplates.cs` — pure, `internal static` body renderers
  for every email (testable without DB). Consumes `EmailTheme`.
- `Services/EmailService.cs` — sending mechanism only: Microsoft Graph,
  CID logo attachment, recipient/BCC handling. Public methods delegate body
  rendering to `EmailTemplates`.

## Wiring

- `EmailService` gains `IWebHostEnvironment` to load + cache the logo PNG bytes
  once; `SendAsync` attaches it as an inline `FileAttachment`
  (`ContentId = "fellside-logo"`, `IsInline = true`) and accepts an optional
  `bccAdmin` flag.
- New `EmailService` methods:
  - `SendDocumentAddedAsync(client, project, documentTitle, portalUrl)`
  - `SendInvoiceAddedAsync(client, project, invoice, portalUrl)`
  - `SendInvoiceStatusChangedAsync(client, project, invoice, portalUrl)`
- `ProjectDocumentService.UploadAsync`, `InvoiceService.UploadAsync` and
  `InvoiceService.UpdateStatusAsync` inject `EmailService`, `NavigationManager`
  and `ILogger`. After the DB save + timeline record they load the project's
  `Client` and send, deep-linking to `/Portal/Projects/{projectId}` via
  `ToAbsoluteUri`.

## Testing

- `EmailTemplateTests` (pure logic, no fixture): assert blue tokens present, no
  orange/indigo, CTA URL embedded, `cid:fellside-logo` referenced, recipient
  detail present. Requires `[assembly: InternalsVisibleTo("FellsideDigital.Tests")]`.
- Build must compile; live Graph sending stays manual (creds required).
