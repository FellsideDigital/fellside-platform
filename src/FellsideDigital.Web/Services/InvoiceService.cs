using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class InvoiceService(
    FellsideDigitalDbContext db,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions,
    IProjectTimelineService timeline,
    EmailService email,
    NavigationManager nav,
    ILogger<InvoiceService> logger) : IInvoiceService
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];

    private static readonly Dictionary<string, string> ContentTypes = new()
    {
        [".pdf"]  = "application/pdf",
        [".png"]  = "image/png",
        [".jpg"]  = "image/jpeg",
        [".jpeg"] = "image/jpeg",
    };

    public async Task<Invoice> UploadAsync(
        Guid projectId, string title, string? description,
        decimal amount, string currency, DateTime? dueAt, IBrowserFile file, string? actorId = null)
    {
        var ext = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"File type '{ext}' is not allowed. Use PDF or an image.");

        var invoiceId = Guid.NewGuid();
        var key = $"invoices/{projectId}/{invoiceId}{ext}";
        var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

        await using var stream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
        await storage.UploadAsync(key, stream, contentType);

        var invoice = new Invoice
        {
            Id          = invoiceId,
            ProjectId   = projectId,
            Title       = title,
            Description = description,
            Amount      = amount,
            Currency    = currency,
            DueAt       = dueAt.HasValue ? DateTime.SpecifyKind(dueAt.Value, DateTimeKind.Utc) : null,
            FilePath    = key,       // S3 object key — not a web URL
            FileName    = file.Name, // original filename for display
            IssuedAt    = DateTime.UtcNow,
            CreatedAt   = DateTime.UtcNow,
            Status      = InvoiceStatus.Sent,
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            projectId, TimelineEventType.InvoiceCreated, $"Invoice issued: {title}",
            TimelineVisibility.ClientVisible, actorId, occurredAt: invoice.IssuedAt);

        await NotifyClientAsync(projectId, (client, project, url) =>
            email.SendInvoiceAddedAsync(client, project, invoice, url));

        return invoice;
    }

    public async Task<Invoice> UpdateAsync(
        Guid id, string title, string? description,
        decimal amount, string currency, DateTime? dueAt,
        IBrowserFile? newFile, bool notifyClient, string? actorId = null)
    {
        var invoice = await db.Invoices.FindAsync(id)
            ?? throw new InvalidOperationException("That invoice no longer exists.");

        invoice.Title       = title;
        invoice.Description = description;
        invoice.Amount      = amount;
        invoice.Currency    = currency;
        invoice.DueAt       = dueAt.HasValue ? DateTime.SpecifyKind(dueAt.Value, DateTimeKind.Utc) : null;

        if (newFile is not null)
        {
            var ext = Path.GetExtension(newFile.Name).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"File type '{ext}' is not allowed. Use PDF or an image.");

            var key = $"invoices/{invoice.ProjectId}/{invoice.Id}{ext}";
            var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

            await using var stream = newFile.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024);
            await storage.UploadAsync(key, stream, contentType);

            // If the extension changed the key changes too, leaving the old object orphaned — remove it.
            var oldPath = invoice.FilePath;
            if (!string.IsNullOrEmpty(oldPath) && oldPath != key)
            {
                try { await storage.DeleteAsync(oldPath); }
                catch (Exception ex)
                {
                    // Non-fatal — an orphaned S3 object is recoverable and must not block the update.
                    logger.LogWarning(ex, "Failed to delete replaced invoice file {Key}", oldPath);
                }
            }

            invoice.FilePath = key;
            invoice.FileName = newFile.Name;
        }

        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            invoice.ProjectId, TimelineEventType.InvoiceUpdated, $"Invoice updated: {title}",
            TimelineVisibility.ClientVisible, actorId);

        if (notifyClient)
            await NotifyClientAsync(invoice.ProjectId, (client, project, url) =>
                email.SendInvoiceUpdatedAsync(client, project, invoice, url));

        return invoice;
    }

    public async Task<string?> GetDownloadUrlAsync(Guid id)
    {
        var invoice = await db.Invoices.FindAsync(id);
        if (invoice?.FilePath is null) return null;

        var expiry = TimeSpan.FromMinutes(storageOptions.Value.PresignedUrlExpiryMinutes);
        return await storage.GetPresignedUrlAsync(invoice.FilePath, expiry);
    }

    public async Task<List<Invoice>> GetForProjectAsync(Guid projectId)
        => await db.Invoices
            .Where(i => i.ProjectId == projectId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();

    public async Task<List<Invoice>> GetForClientAsync(string clientId)
        => await db.Invoices
            .Include(i => i.Project)
            .Where(i => i.Project!.ClientId == clientId)
            .OrderByDescending(i => i.IssuedAt)
            .ToListAsync();

    public async Task<Invoice?> GetByIdAsync(Guid id)
        => await db.Invoices
            .Include(i => i.Project)
                .ThenInclude(p => p!.Client)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task UpdateStatusAsync(Guid id, InvoiceStatus status, string? actorId = null)
    {
        var invoice = await db.Invoices.FindAsync(id);
        if (invoice is null) return;

        var changed = invoice.Status != status;
        invoice.Status = status;
        if (status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (!changed) return;

        // Draft is an internal pre-send state — no client-facing timeline entry for it.
        (TimelineEventType Type, string Summary)? evt = status switch
        {
            InvoiceStatus.Paid    => (TimelineEventType.InvoicePaid,    $"Invoice paid: {invoice.Title}"),
            InvoiceStatus.Overdue => (TimelineEventType.InvoiceOverdue, $"Invoice overdue: {invoice.Title}"),
            InvoiceStatus.Sent    => (TimelineEventType.InvoiceSent,    $"Invoice sent: {invoice.Title}"),
            _                     => null
        };

        if (evt is { } e)
            await timeline.RecordAsync(invoice.ProjectId, e.Type, e.Summary, TimelineVisibility.ClientVisible, actorId);

        // Notify the client when an invoice becomes Sent or Overdue. Draft is internal
        // and Paid is bookkeeping — neither warrants a client email.
        if (status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
            await NotifyClientAsync(invoice.ProjectId, (client, project, url) =>
                email.SendInvoiceStatusChangedAsync(client, project, invoice, url));
    }

    /// <summary>
    /// Emails the project's client (admin BCC'd) about an invoice change. Never throws —
    /// a notification failure must not break the upload or status change that triggered it.
    /// </summary>
    private async Task NotifyClientAsync(Guid projectId, Func<ApplicationUser, ClientProject, string, Task> send)
    {
        try
        {
            var project = await db.ClientProjects
                .Include(p => p.Client)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project?.Client?.Email is not { Length: > 0 }) return;

            var url = nav.ToAbsoluteUri($"/Portal/Projects/{projectId}").ToString();
            await send(project.Client, project, url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invoice notification for project {ProjectId}", projectId);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await db.Invoices.FindAsync(id);
        if (invoice is null) return;

        if (!string.IsNullOrEmpty(invoice.FilePath))
        {
            try { await storage.DeleteAsync(invoice.FilePath); }
            catch (Exception ex)
            {
                // Log but don't block the DB delete — orphaned S3 objects are recoverable
                Console.Error.WriteLine($"S3 delete failed for {invoice.FilePath}: {ex.Message}");
            }
        }

        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync();
    }
}
