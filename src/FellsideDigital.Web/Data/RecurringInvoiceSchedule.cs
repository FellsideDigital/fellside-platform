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

    /// <summary>Preferred issue day (1–31); clamped to the last day of shorter months.</summary>
    public int DayOfMonth { get; set; } = 1;

    /// <summary>Days after issue that the generated invoice falls due.</summary>
    public int DueDays { get; set; } = 14;

    public bool IsActive { get; set; } = true;

    /// <summary>UTC date the next invoice should be issued on (time component ignored).</summary>
    public DateTime NextIssueDate { get; set; }

    public DateTime? LastIssuedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }
    public ClientProject? Project { get; set; }
}
