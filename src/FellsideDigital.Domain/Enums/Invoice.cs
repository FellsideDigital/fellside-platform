namespace FellsideDigital.Domain.Enums;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue
}

/// <summary>
/// The escalation step of an automated invoice reminder email. Ordered — each
/// invoice moves through these at most once, so automated chasing is capped.
/// </summary>
public enum InvoiceReminderKind
{
    /// <summary>Friendly heads-up a few days before the due date.</summary>
    Upcoming = 1,

    /// <summary>First nudge once the due date has passed.</summary>
    Overdue = 2,

    /// <summary>Last automated nudge a week after the due date.</summary>
    Final = 3
}