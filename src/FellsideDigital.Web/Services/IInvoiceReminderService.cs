namespace FellsideDigital.Web.Services;

public interface IInvoiceReminderService
{
    /// <summary>
    /// Sends staged reminder emails for unpaid invoices with a due date and flips
    /// Sent → Overdue once the due date passes. Each invoice receives at most three
    /// automated reminders ever (due-soon, overdue, final) — the stage is persisted
    /// before sending, so re-runs and restarts can't double-email. Returns the
    /// number of reminders sent.
    /// </summary>
    Task<int> ProcessRemindersAsync(DateTime utcNow);
}
