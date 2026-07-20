using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

/// <summary>
/// Pure earnings maths — no fixture, runs without Docker. "Total earned" is paid one-off
/// invoices plus the estimated retainer collections, never double-counting schedule-generated
/// invoices.
/// </summary>
public class InvoiceEarningsTests
{
    private static readonly DateTime AsOf = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    private static Invoice Inv(InvoiceStatus status, decimal amount, Guid? scheduleId = null) => new()
    {
        Id = Guid.NewGuid(), Amount = amount, Status = status, ScheduleId = scheduleId,
    };

    private static RecurringInvoiceSchedule Schedule(decimal amount, DateTime firstPayment, bool active = true) => new()
    {
        Id = Guid.NewGuid(), Amount = amount, FirstPaymentDate = firstPayment, IsActive = active,
    };

    [Fact]
    public void SumsPaidOneOffInvoices_Only()
    {
        var invoices = new[]
        {
            Inv(InvoiceStatus.Paid, 100m),
            Inv(InvoiceStatus.Paid, 250m),
            Inv(InvoiceStatus.Sent, 999m),     // not paid — excluded
            Inv(InvoiceStatus.Overdue, 999m),  // not paid — excluded
            Inv(InvoiceStatus.Draft, 999m),    // not paid — excluded
        };

        Assert.Equal(350m, InvoiceEarnings.TotalEarned(invoices, [], AsOf));
    }

    [Fact]
    public void ExcludesScheduleGeneratedInvoices_FromPaidSum_ToAvoidDoubleCounting()
    {
        // A retainer running three months (first payment + 2 anniversaries = 3 payments).
        var schedule = Schedule(100m, AsOf.AddMonths(-2));
        var invoices = new[]
        {
            Inv(InvoiceStatus.Paid, 100m, scheduleId: schedule.Id), // counted via the estimate, not here
            Inv(InvoiceStatus.Paid, 500m),                          // genuine one-off — counted
        };

        // 3 × £100 recurring estimate + £500 one-off, and NOT the £100 schedule-generated paid invoice.
        Assert.Equal(800m, InvoiceEarnings.TotalEarned(invoices, [schedule], AsOf));
    }

    [Fact]
    public void AddsRecurringEstimate_AcrossSchedules()
    {
        var a = Schedule(100m, AsOf.AddMonths(-1)); // 2 payments = £200
        var b = Schedule(50m, AsOf);                // 1 payment  = £50

        Assert.Equal(250m, InvoiceEarnings.TotalEarned([], [a, b], AsOf));
    }

    [Fact]
    public void IgnoresRetainersWhoseFirstPaymentIsInTheFuture()
    {
        var future = Schedule(100m, AsOf.AddMonths(2));

        Assert.Equal(0m, InvoiceEarnings.TotalEarned([], [future], AsOf));
    }

    [Fact]
    public void EmptyInputs_YieldZero()
    {
        Assert.Equal(0m, InvoiceEarnings.TotalEarned([], [], AsOf));
    }
}
