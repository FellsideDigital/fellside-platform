namespace FellsideDigital.Web.Data;

/// <summary>
/// A monthly billing schedule for a project (e.g. a retainer). The invoice
/// automation worker turns each due schedule into a real <see cref="Invoice"/>
/// and advances <see cref="NextIssueDate"/>, so generation is idempotent —
/// re-running the worker on the same day produces nothing new.
/// </summary>
public class RecurringInvoiceSchedule
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Day of the month (1–31) this schedule is issued and payment is collected. Seeded from
    /// the global <c>Billing:PaymentDayOfMonth</c> on creation, but overridable per customer
    /// (e.g. some are billed on the 18th rather than the 1st). Days 29–31 are clamped to the
    /// last day of shorter months.
    /// </summary>
    public int PaymentDayOfMonth { get; set; } = 1;

    /// <summary>
    /// UTC date the next invoice is issued and payment is collected (time component
    /// ignored). Always falls on this schedule's <see cref="PaymentDayOfMonth"/>.
    /// </summary>
    public DateTime NextIssueDate { get; set; }

    public DateTime? LastIssuedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }
}
