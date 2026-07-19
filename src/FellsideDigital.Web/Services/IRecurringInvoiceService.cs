using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IRecurringInvoiceService
{
    Task<RecurringInvoiceSchedule> CreateAsync(
        Guid projectId, string title, string? description, decimal amount, string currency,
        int dayOfMonth, int dueDays, string? actorId = null);

    Task<RecurringInvoiceSchedule> UpdateAsync(
        Guid id, string title, string? description, decimal amount, string currency,
        int dayOfMonth, int dueDays);

    Task SetActiveAsync(Guid id, bool isActive);
    Task DeleteAsync(Guid id);
    Task<List<RecurringInvoiceSchedule>> GetForClientAsync(string clientId);

    /// <summary>
    /// Issues an invoice for every active schedule whose <c>NextIssueDate</c> has been
    /// reached, advancing each schedule one month per invoice. Idempotent — re-running
    /// on the same day generates nothing further. Returns the number issued.
    /// </summary>
    Task<int> GenerateDueInvoicesAsync(DateTime utcNow);
}
