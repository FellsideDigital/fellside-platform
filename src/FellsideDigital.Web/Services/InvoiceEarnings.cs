using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

/// <summary>
/// Revenue actually earned from a customer: one-off invoices marked <see cref="InvoiceStatus.Paid"/>
/// plus the estimated Direct-Debit collections from recurring retainers.
/// <para>
/// Retainer income is tracked as an estimate (<see cref="RecurringInvoiceService.CollectedFor"/>)
/// because those invoices are collected via GoCardless and aren't manually marked Paid. Invoices
/// generated from a schedule are therefore excluded from the paid sum — they're represented by the
/// schedule estimate — so retainer income is never double-counted.
/// </para>
/// </summary>
public static class InvoiceEarnings
{
    public static decimal TotalEarned(
        IEnumerable<Invoice> invoices,
        IEnumerable<RecurringInvoiceSchedule> schedules,
        DateTime asOf)
    {
        var paidOneOff = invoices
            .Where(i => i.ScheduleId is null && i.Status == InvoiceStatus.Paid)
            .Sum(i => i.Amount);

        var recurringCollected = schedules.Sum(s => RecurringInvoiceService.CollectedFor(s, asOf));

        return paidOneOff + recurringCollected;
    }
}
