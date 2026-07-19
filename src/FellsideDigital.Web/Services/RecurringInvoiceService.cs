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
    ILogger<RecurringInvoiceService> logger) : IRecurringInvoiceService
{
    public async Task<RecurringInvoiceSchedule> CreateAsync(
        Guid projectId, string title, string? description, decimal amount, string currency,
        int dayOfMonth, int dueDays, string? actorId = null)
    {
        Validate(title, amount, dayOfMonth, dueDays);

        var schedule = new RecurringInvoiceSchedule
        {
            Id            = Guid.NewGuid(),
            ProjectId     = projectId,
            Title         = title,
            Description   = description,
            Amount        = amount,
            Currency      = currency,
            DayOfMonth    = dayOfMonth,
            DueDays       = dueDays,
            IsActive      = true,
            NextIssueDate = FirstIssueDate(DateTime.UtcNow, dayOfMonth),
            CreatedAt     = DateTime.UtcNow,
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
        Guid id, string title, string? description, decimal amount, string currency,
        int dayOfMonth, int dueDays)
    {
        Validate(title, amount, dayOfMonth, dueDays);

        var schedule = await db.RecurringInvoiceSchedules.FindAsync(id)
            ?? throw new InvalidOperationException("That recurring invoice no longer exists.");

        // If the issue day changed, re-anchor the next issue to the new day so the
        // change takes effect from the upcoming cycle rather than the one after.
        if (schedule.DayOfMonth != dayOfMonth)
            schedule.NextIssueDate = FirstIssueDate(DateTime.UtcNow, dayOfMonth);

        schedule.Title       = title;
        schedule.Description = description;
        schedule.Amount      = amount;
        schedule.Currency    = currency;
        schedule.DayOfMonth  = dayOfMonth;
        schedule.DueDays     = dueDays;

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
            schedule.NextIssueDate = FirstIssueDate(DateTime.UtcNow, schedule.DayOfMonth);

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var schedule = await db.RecurringInvoiceSchedules.FindAsync(id);
        if (schedule is null) return;

        db.RecurringInvoiceSchedules.Remove(schedule);
        await db.SaveChangesAsync();
    }

    public async Task<List<RecurringInvoiceSchedule>> GetForClientAsync(string clientId)
        => await db.RecurringInvoiceSchedules
            .Include(s => s.Project)
            .Where(s => s.Project!.ClientId == clientId)
            .OrderBy(s => s.CreatedAt)
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
                    DueAt       = DateTime.SpecifyKind(period.Date.AddDays(schedule.DueDays), DateTimeKind.Utc),
                    Status      = InvoiceStatus.Sent,
                };

                db.Invoices.Add(invoice);
                schedule.NextIssueDate = NextMonthlyDate(period, schedule.DayOfMonth);
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

    private static void Validate(string title, decimal amount, int dayOfMonth, int dueDays)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidOperationException("Give the recurring invoice a title.");
        if (amount <= 0)
            throw new InvalidOperationException("The amount must be greater than zero.");
        if (dayOfMonth is < 1 or > 31)
            throw new InvalidOperationException("The issue day must be between 1 and 31.");
        if (dueDays is < 0 or > 90)
            throw new InvalidOperationException("Days until due must be between 0 and 90.");
    }

    /// <summary>The first occurrence of <paramref name="dayOfMonth"/> on or after today (UTC).</summary>
    internal static DateTime FirstIssueDate(DateTime utcNow, int dayOfMonth)
    {
        var today = utcNow.Date;
        var thisMonth = ClampedDate(today.Year, today.Month, dayOfMonth);
        return thisMonth >= today ? thisMonth : NextMonthlyDate(thisMonth, dayOfMonth);
    }

    /// <summary>The schedule's day in the month after <paramref name="from"/>, clamped to shorter months.</summary>
    internal static DateTime NextMonthlyDate(DateTime from, int dayOfMonth)
    {
        var firstOfNext = new DateTime(from.Year, from.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);
        return ClampedDate(firstOfNext.Year, firstOfNext.Month, dayOfMonth);
    }

    private static DateTime ClampedDate(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)), 0, 0, 0, DateTimeKind.Utc);
}
