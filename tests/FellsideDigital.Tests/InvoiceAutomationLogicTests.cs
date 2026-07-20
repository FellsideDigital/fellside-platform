using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

/// <summary>
/// Pure date/stage logic for invoice automation — no fixture, runs without Docker.
/// The DB-backed behaviour lives in RecurringInvoiceServiceTests / InvoiceReminderServiceTests.
/// </summary>
public class InvoiceAutomationLogicTests
{
    private static readonly DateTime Due = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(2026, 1, 31, 31, 2026, 2, 28)] // clamped to short month
    [InlineData(2026, 2, 28, 31, 2026, 3, 31)] // restored to the anchor day after
    [InlineData(2026, 6, 15, 15, 2026, 7, 15)] // plain month step
    [InlineData(2027, 12, 1, 1, 2028, 1, 1)]   // year rollover
    public void NextMonthlyDate_ClampsToShorterMonths(
        int y, int m, int d, int anchorDay, int ey, int em, int ed)
    {
        var next = RecurringInvoiceService.NextMonthlyDate(
            new DateTime(y, m, d, 0, 0, 0, DateTimeKind.Utc), anchorDay);
        Assert.Equal(new DateTime(ey, em, ed, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void FirstIssueDate_UsesThisMonth_WhenDayNotYetPassed()
    {
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 7, 15, 0, 0, 0, DateTimeKind.Utc),
            RecurringInvoiceService.FirstIssueDate(now, 15));
        Assert.Equal(new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc),
            RecurringInvoiceService.FirstIssueDate(now, 10));
        Assert.Equal(new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc),
            RecurringInvoiceService.FirstIssueDate(now, 5));
    }

    [Fact]
    public void ShiftToDay_MovesWithinScheduledMonth_WhenNewDayStillFuture()
    {
        // Next issue Aug 1; admin moves the customer to the 18th mid-July — bill shifts to Aug 18.
        var now = new DateTime(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);
        var shifted = RecurringInvoiceService.ShiftToDay(
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), 18, now);
        Assert.Equal(new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), shifted);
    }

    [Fact]
    public void ShiftToDay_AdvancesAMonth_RatherThanBackIssuingAPastDay()
    {
        // Next issue Aug 18; lowering to the 1st on Aug 10 must NOT land on the already-passed Aug 1.
        var now = new DateTime(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
        var shifted = RecurringInvoiceService.ShiftToDay(
            new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc), 1, now);
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), shifted);
    }

    [Fact]
    public void ShiftToDay_ClampsToShortMonth()
    {
        var now = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var shifted = RecurringInvoiceService.ShiftToDay(
            new DateTime(2027, 2, 1, 0, 0, 0, DateTimeKind.Utc), 31, now);
        Assert.Equal(new DateTime(2027, 2, 28, 0, 0, 0, DateTimeKind.Utc), shifted);
    }

    [Theory]
    [InlineData(2024, 3, 1, 2026, 7, 20, 29)]  // ~2.5 years of monthly payments, this month's already collected
    [InlineData(2026, 6, 15, 2026, 7, 20, 2)]  // anniversary day passed this month → both June and July count
    [InlineData(2026, 6, 25, 2026, 7, 20, 1)]  // anniversary not yet reached this month → only June counts
    [InlineData(2026, 7, 1, 2026, 7, 20, 1)]   // first payment this month
    [InlineData(2026, 7, 1, 2026, 7, 1, 1)]    // exactly on the first payment day
    [InlineData(2026, 8, 1, 2026, 7, 20, 0)]   // first payment still in the future
    public void PaymentsCollected_CountsMonthlyAnniversaries(
        int sy, int sm, int sd, int ay, int am, int ad, int expected)
    {
        var first = new DateTime(sy, sm, sd, 0, 0, 0, DateTimeKind.Utc);
        var asOf  = new DateTime(ay, am, ad, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(expected, RecurringInvoiceService.PaymentsCollected(first, asOf));
    }

    [Theory]
    [InlineData(0, -4, null)]                              // too early
    [InlineData(0, -3, InvoiceReminderKind.Upcoming)]      // lead window opens
    [InlineData(0, 0, InvoiceReminderKind.Upcoming)]       // due day still counts as upcoming
    [InlineData(1, 0, null)]                               // already reminded this stage
    [InlineData(1, 1, InvoiceReminderKind.Overdue)]        // day after due
    [InlineData(0, 1, InvoiceReminderKind.Overdue)]        // skips straight past upcoming
    [InlineData(2, 6, null)]                               // overdue sent, final not yet due
    [InlineData(2, 7, InvoiceReminderKind.Final)]          // a week after due
    [InlineData(0, 10, InvoiceReminderKind.Final)]         // long-overdue gets one email, not three
    [InlineData(3, 40, null)]                              // capped
    public void NextReminder_FollowsStagePolicy(int stage, int daysFromDue, InvoiceReminderKind? expected)
    {
        var invoice = new Invoice { DueAt = Due, ReminderStage = stage };
        Assert.Equal(expected, InvoiceReminderService.NextReminder(invoice, Due.AddDays(daysFromDue).Date));
    }

    [Fact]
    public void NextReminder_IsNull_WithoutDueDate()
    {
        Assert.Null(InvoiceReminderService.NextReminder(new Invoice { DueAt = null }, Due));
    }

    [Fact]
    public void DelayUntilNextRun_TargetsNineUtc()
    {
        var beforeNine = new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc);
        Assert.Equal(TimeSpan.FromHours(3), InvoiceAutomationWorker.DelayUntilNextRun(beforeNine));

        var afterNine = new DateTime(2026, 7, 20, 10, 30, 0, DateTimeKind.Utc);
        Assert.Equal(TimeSpan.FromHours(22.5), InvoiceAutomationWorker.DelayUntilNextRun(afterNine));
    }
}
