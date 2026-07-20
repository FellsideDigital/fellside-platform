using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IRecurringInvoiceService
{
    Task<RecurringInvoiceSchedule> CreateAsync(
        Guid projectId, string title, string? description, decimal amount, string currency,
        int? paymentDay = null, DateTime? firstPaymentDate = null, string? actorId = null);

    Task<RecurringInvoiceSchedule> UpdateAsync(
        Guid id, string title, string? description, decimal amount, string currency, int paymentDay,
        DateTime firstPaymentDate);

    Task SetActiveAsync(Guid id, bool isActive);
    Task DeleteAsync(Guid id);
    Task<List<RecurringInvoiceSchedule>> GetForClientAsync(string clientId);

    /// <summary>Every recurring schedule across all clients, with project and client loaded — for the admin-wide invoices view.</summary>
    Task<List<RecurringInvoiceSchedule>> GetAllSchedulesAsync();

    /// <summary>
    /// Estimated total collected for a schedule so far: the number of monthly payments due since
    /// its <c>FirstPaymentDate</c> (assuming each Direct Debit succeeded) times the monthly amount.
    /// A paused schedule stops accruing at its last issued invoice.
    /// </summary>
    decimal CollectedToDate(RecurringInvoiceSchedule schedule, DateTime asOf);

    /// <summary>
    /// Issues an invoice for every active schedule whose <c>NextIssueDate</c> has been
    /// reached, advancing each schedule one month per invoice. Generated invoices are
    /// issued and due on that schedule's own <c>PaymentDayOfMonth</c>.
    /// Idempotent — re-running on the same day generates nothing further. Returns the
    /// number issued.
    /// </summary>
    Task<int> GenerateDueInvoicesAsync(DateTime utcNow);
}
