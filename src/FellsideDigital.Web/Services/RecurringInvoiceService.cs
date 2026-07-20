using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class RecurringInvoiceService(
    FellsideDigitalDbContext db,
    IProjectTimelineService timeline,
    IEmailService email,
    IOptions<SiteSettings> siteOptions,
    IOptions<BillingSettings> billingOptions,
    ILogger<RecurringInvoiceService> logger) : IRecurringInvoiceService
{
    private int PaymentDay => billingOptions.Value.PaymentDayOfMonth;

    public async Task<RecurringInvoiceSchedule> CreateAsync(
        Guid projectId, string title, string? description, decimal amount, string currency,
        int? paymentDay = null, DateTime? firstPaymentDate = null, string? actorId = null)
    {
        Validate(title, amount);
        var day = paymentDay ?? PaymentDay;
        ValidateDay(day);

        var firstIssue = FirstIssueDate(DateTime.UtcNow, day);
        var schedule = new RecurringInvoiceSchedule
        {
            Id                = Guid.NewGuid(),
            ProjectId         = projectId,
            Title             = title,
            Description       = description,
            Amount            = amount,
            Currency          = currency,
            IsActive          = true,
            PaymentDayOfMonth = day,
            NextIssueDate     = firstIssue,
            // First payment defaults to the first collection; admins can back-date it for
            // retainers that started before the platform. Stored as a UTC date.
            FirstPaymentDate  = DateTime.SpecifyKind((firstPaymentDate ?? firstIssue).Date, DateTimeKind.Utc),
            CreatedAt         = DateTime.UtcNow,
        };

        db.RecurringInvoiceSchedules.Add(schedule);
        await db.SaveChangesAsync();

        await timeline.RecordAsync(
            projectId, TimelineEventType.InvoiceCreated,
            $"Recurring invoice scheduled: {title} ({currency} {amount:N2}/month)",
            TimelineVisibility.Internal, actorId);

        return schedule;
    }

    public async Task<RecurringInvoiceSchedule> UpdateAsync(
        Guid id, string title, string? description, decimal amount, string currency, int paymentDay,
        DateTime firstPaymentDate)
    {
        Validate(title, amount);
        ValidateDay(paymentDay);

        var schedule = await db.RecurringInvoiceSchedules.FindAsync(id)
            ?? throw new InvalidOperationException("That recurring invoice no longer exists.");

        schedule.Title            = title;
        schedule.Description      = description;
        schedule.Amount           = amount;
        schedule.Currency         = currency;
        schedule.FirstPaymentDate = DateTime.SpecifyKind(firstPaymentDate.Date, DateTimeKind.Utc);

        if (schedule.PaymentDayOfMonth != paymentDay)
        {
            schedule.PaymentDayOfMonth = paymentDay;
            // Move the next collection onto the new day without re-issuing a month already billed.
            schedule.NextIssueDate = ShiftToDay(schedule.NextIssueDate, paymentDay, DateTime.UtcNow);
        }

        await db.SaveChangesAsync();
        return schedule;
    }

    public async Task SetActiveAsync(Guid id, bool isActive)
    {
        var schedule = await db.RecurringInvoiceSchedules.FindAsync(id);
        if (schedule is null || schedule.IsActive == isActive) return;

        schedule.IsActive = isActive;

        // Re-anchor on resume so a long pause doesn't back-issue months of invoices.
        if (isActive)
            schedule.NextIssueDate = FirstIssueDate(DateTime.UtcNow, schedule.PaymentDayOfMonth);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var schedule = await db.RecurringInvoiceSchedules.FindAsync(id);
        if (schedule is null) return;

        db.RecurringInvoiceSchedules.Remove(schedule);
        await db.SaveChangesAsync();
    }

    public decimal CollectedToDate(RecurringInvoiceSchedule schedule, DateTime asOf)
        => CollectedFor(schedule, asOf);

    /// <summary>
    /// Pure estimate of the total collected for a schedule as of <paramref name="asOf"/>, so it can be
    /// reused (e.g. by <see cref="InvoiceEarnings"/>) without constructing the service. A paused schedule
    /// stops accruing at its last issued invoice; an active one accrues to now.
    /// </summary>
    internal static decimal CollectedFor(RecurringInvoiceSchedule schedule, DateTime asOf)
    {
        var cutoff = schedule.IsActive ? asOf : (schedule.LastIssuedAt ?? asOf);
        return PaymentsCollected(schedule.FirstPaymentDate, cutoff) * schedule.Amount;
    }

    /// <summary>
    /// Number of monthly payments collected from <paramref name="firstPayment"/> up to and
    /// including <paramref name="asOf"/> — 0 if the first payment is still in the future.
    /// </summary>
    internal static int PaymentsCollected(DateTime firstPayment, DateTime asOf)
    {
        var start = firstPayment.Date;
        var today = asOf.Date;
        if (today < start) return 0;

        var months = (today.Year - start.Year) * 12 + (today.Month - start.Month);
        if (today.Day < start.Day) months--; // this month's anniversary hasn't arrived yet
        return months + 1;                    // include the first payment itself
    }

    public async Task<List<RecurringInvoiceSchedule>> GetForClientAsync(string clientId)
        => await db.RecurringInvoiceSchedules
            .Include(s => s.Project)
            .Where(s => s.Project!.ClientId == clientId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

    public async Task<List<RecurringInvoiceSchedule>> GetAllSchedulesAsync()
        => await db.RecurringInvoiceSchedules
            .Include(s => s.Project)
                .ThenInclude(p => p!.Client)
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync();

    public async Task<int> GenerateDueInvoicesAsync(DateTime utcNow)
    {
        var today = utcNow.Date;
        var due = await db.RecurringInvoiceSchedules
            .Include(s => s.Project)
                .ThenInclude(p => p!.Client)
            .Where(s => s.IsActive && s.NextIssueDate <= today)
            .ToListAsync();

        var issued = 0;
        foreach (var schedule in due)
        {
            // A schedule that was missed for several cycles (e.g. downtime) issues one
            // invoice per missed month — each period is still owed.
            while (schedule.NextIssueDate.Date <= today)
            {
                var period = schedule.NextIssueDate;
                var invoice = new Invoice
                {
                    Id          = Guid.NewGuid(),
                    ProjectId   = schedule.ProjectId,
                    ScheduleId  = schedule.Id,
                    Title       = $"{schedule.Title} — {period:MMMM yyyy}",
                    Description = schedule.Description,
                    Amount      = schedule.Amount,
                    Currency    = schedule.Currency,
                    IssuedAt    = utcNow,
                    CreatedAt   = utcNow,
                    // Payment is collected on the issue day itself, so the invoice is
                    // due the day it appears. The issue email is the client's notice —
                    // start past the "due soon" reminder stage so the same worker run
                    // doesn't immediately send a second email.
                    DueAt         = DateTime.SpecifyKind(period.Date, DateTimeKind.Utc),
                    Status        = InvoiceStatus.Sent,
                    ReminderStage = (int)InvoiceReminderKind.Upcoming,
                };

                db.Invoices.Add(invoice);
                schedule.NextIssueDate = NextMonthlyDate(period, schedule.PaymentDayOfMonth);
                schedule.LastIssuedAt  = utcNow;

                // Persist before emailing so a failed send can't cause a duplicate
                // invoice on the next run.
                await db.SaveChangesAsync();

                await timeline.RecordAsync(
                    schedule.ProjectId, TimelineEventType.InvoiceCreated,
                    $"Invoice issued: {invoice.Title}",
                    TimelineVisibility.ClientVisible, actorId: null, occurredAt: utcNow);

                await NotifyClientAsync(schedule.Project, invoice);
                issued++;
            }
        }

        return issued;
    }

    /// <summary>Emails the client about the generated invoice. Never throws.</summary>
    private async Task NotifyClientAsync(ClientProject? project, Invoice invoice)
    {
        try
        {
            if (project?.Client?.Email is not { Length: > 0 }) return;
            var url = siteOptions.Value.PortalProjectUrl(project.Id);
            await email.SendInvoiceAddedAsync(project.Client, project, invoice, url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send generated-invoice notification for invoice {InvoiceId}", invoice.Id);
        }
    }

    private static void Validate(string title, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Give the recurring invoice a title.");
        if (amount <= 0)
            throw new InvalidOperationException("The amount must be greater than zero.");
    }

    private static void ValidateDay(int dayOfMonth)
    {
        if (dayOfMonth is < 1 or > 31)
            throw new InvalidOperationException("The payment day must be between 1 and 31.");
    }

    /// <summary>
    /// Moves an existing <c>NextIssueDate</c> onto <paramref name="newDay"/> within the month it
    /// already sits in, advancing to the following month only if that lands in the past — so a
    /// day change never back-issues a period that was already (or nearly) billed.
    /// </summary>
    internal static DateTime ShiftToDay(DateTime nextIssue, int newDay, DateTime utcNow)
    {
        var target = ClampedDate(nextIssue.Year, nextIssue.Month, newDay);
        return target.Date >= utcNow.Date ? target : NextMonthlyDate(target, newDay);
    }

    /// <summary>The first occurrence of <paramref name="dayOfMonth"/> on or after today (UTC).</summary>
    internal static DateTime FirstIssueDate(DateTime utcNow, int dayOfMonth)
    {
        var today = utcNow.Date;
        var thisMonth = ClampedDate(today.Year, today.Month, dayOfMonth);
        return thisMonth >= today ? thisMonth : NextMonthlyDate(thisMonth, dayOfMonth);
    }

    /// <summary>The payment day in the month after <paramref name="from"/>, clamped to shorter months.</summary>
    internal static DateTime NextMonthlyDate(DateTime from, int dayOfMonth)
    {
        var firstOfNext = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return ClampedDate(firstOfNext.Year, firstOfNext.Month, dayOfMonth);
    }

    private static DateTime ClampedDate(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)), 0, 0, 0, DateTimeKind.Utc);
}
