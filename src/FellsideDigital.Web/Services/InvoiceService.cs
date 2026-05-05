using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class InvoiceService(
    FellsideDigitalDbContext db,
    IStorageService storage,
    IOptions<StorageSettings> storageOptions) : IInvoiceService
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
        decimal amount, string currency, DateTime? dueAt, IBrowserFile file)
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

    public async Task UpdateStatusAsync(Guid id, InvoiceStatus status)
    {
        var invoice = await db.Invoices.FindAsync(id);
        if (invoice is null) return;
        invoice.Status = status;
        if (status == InvoiceStatus.Paid)
            invoice.PaidAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
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
