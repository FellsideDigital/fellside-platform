# Edit Invoice — Design

**Date:** 2026-06-11
**Status:** Approved

## Goal

Let an admin open an existing invoice, edit its details and optionally replace the
attached document, and (optionally) notify the client by email that the invoice was
updated.

## Decisions

- **Edit scope:** Document + details (title, amount, currency, due date).
- **Document is optional on edit** — the modal doubles as a details editor; leaving the
  file empty keeps the current document.
- **Email trigger:** "Notify client by email" checkbox in the modal, default **on**.
- **Edit UI:** `Modal` dialog opened from an "Edit" link per row.
- **Status is not editable in the modal** — it is already managed by the inline status
  dropdown on the row.

## Changes

### 1. Service layer (`IInvoiceService` / `InvoiceService`)

New method:

```csharp
Task<Invoice> UpdateAsync(Guid id, string title, string? description, decimal amount,
    string currency, DateTime? dueAt, IBrowserFile? newFile, bool notifyClient,
    string? actorId = null);
```

Behaviour:

- Load the invoice (return/throw if missing).
- Update `Title`, `Description`, `Amount`, `Currency`, `DueAt` (DueAt normalised to UTC
  like `UploadAsync`).
- If `newFile` is provided:
  - Validate extension against existing `AllowedExtensions` (throws
    `InvalidOperationException` with a user-facing message otherwise).
  - Upload to the same key scheme `invoices/{projectId}/{id}{ext}` with the mapped
    content type.
  - If the new key differs from the old `FilePath` (extension changed), delete the old
    S3 object (best-effort; failure logged, not fatal — mirrors `DeleteAsync`).
  - Update `FilePath` and `FileName`.
- If `newFile` is null, the document is left untouched.
- `SaveChangesAsync`.
- Record a client-visible timeline event `TimelineEventType.InvoiceUpdated`
  (`"Invoice updated: {title}"`).
- If `notifyClient`, call the existing `NotifyClientAsync` helper with
  `email.SendInvoiceUpdatedAsync(...)`. Notification failures never break the save.

### 2. Domain

Add `[Display(Name = "Invoice updated")] InvoiceUpdated` to `TimelineEventType`
(no schema change). Add an icon/tone mapping in `TimelineEventPresenter`.

### 3. Email

- `EmailService.SendInvoiceUpdatedAsync(client, project, invoice, portalUrl)` — subject
  e.g. `"Invoice updated for your {project.Name} project"`, `bccAdmin: true`.
- `EmailTemplates.InvoiceUpdated(...)` mirroring `InvoiceAdded`, reusing `InvoiceRows`.
  Heading "Your invoice has been updated".

### 4. UI (`Admin/Clients/Invoices.razor` + `.razor.cs`)

- Add an "Edit" link per row alongside Download/Delete.
- Edit opens a `Modal` with the form: Title, Amount, Currency, Due date, optional
  `InputFile` (label: "Replace document — leave empty to keep current", current filename
  shown), and a "Notify client by email" checkbox (default on).
- Save via `SpinnerButton` → `UpdateAsync` → close modal, reload list, `Toasts.Success`.
- Errors via `ErrorHandling.LogAndDescribe`, shown in an `AlertBanner` inside the modal.

### 5. Tests (`tests/FellsideDigital.Tests`, Docker-backed)

- Update without a file keeps the existing document and updates the details.
- Update with a file replaces the document; old key removed when the extension changes.
- `notifyClient = false` sends no email.
```
