using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class Invoice
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Sent;
    public string? FilePath { get; set; }
    public string? FileName { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }
}
