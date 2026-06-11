using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Services;

public interface IInvoiceService
{
    Task<Invoice> UploadAsync(Guid projectId, string title, string? description, decimal amount, string currency, DateTime? dueAt, IBrowserFile file, string? actorId = null);

    /// <summary>
    /// Updates an invoice's details and, when <paramref name="newFile"/> is supplied, replaces its
    /// attached document. When <paramref name="notifyClient"/> is true the project's client is emailed.
    /// </summary>
    Task<Invoice> UpdateAsync(Guid id, string title, string? description, decimal amount, string currency, DateTime? dueAt, IBrowserFile? newFile, bool notifyClient, string? actorId = null);
    Task<List<Invoice>> GetForProjectAsync(Guid projectId);
    Task<List<Invoice>> GetForClientAsync(string clientId);
    Task<Invoice?> GetByIdAsync(Guid id);
    Task UpdateStatusAsync(Guid id, InvoiceStatus status, string? actorId = null);
    Task DeleteAsync(Guid id);

    /// <summary>Returns a time-limited presigned download URL for the invoice file, or null if no file is attached.</summary>
    Task<string?> GetDownloadUrlAsync(Guid id);
}
