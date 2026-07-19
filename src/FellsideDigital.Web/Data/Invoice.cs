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

    /// <summary>
    /// Highest automated reminder already sent (0 = none, 1 = due-soon, 2 = overdue,
    /// 3 = final). Monotonic — the reminder worker only moves it forward, which caps
    /// automated chasing at three emails per invoice.
    /// </summary>
    public int ReminderStage { get; set; }

    public DateTime? LastReminderAt { get; set; }

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }

    /// <summary>Set when this invoice was generated from a recurring schedule.</summary>
    public Guid? ScheduleId { get; set; }
    public RecurringInvoiceSchedule? Schedule { get; set; }
}
