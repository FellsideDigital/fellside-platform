namespace FellsideDigital.Web.Models;

/// <summary>
/// Billing policy shared by every client. Payment for recurring invoices is
/// collected (e.g. by Direct Debit) on the same day of the month for all
/// customers; recurring invoices are issued on that day with it as their due
/// date. Days 29–31 are clamped to the last day of shorter months.
/// </summary>
public class BillingSettings
{
    public int PaymentDayOfMonth { get; set; } = 1;
}
