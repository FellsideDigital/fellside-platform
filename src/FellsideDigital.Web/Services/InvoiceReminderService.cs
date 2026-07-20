using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Services;

public class InvoiceReminderService(
    FellsideDigitalDbContext db,
    IProjectTimelineService timeline,
    IEmailService email,
    IOptions<SiteSettings> siteOptions,
    ILogger<InvoiceReminderService> logger) : IInvoiceReminderService
{
    /// <summary>Days before the due date that the "due soon" reminder goes out.</summary>
    private const int UpcomingLeadDays = 3;

    /// <summary>Days after the due date that the final reminder goes out.</summary>
    private const int FinalDelayDays = 7;

    public async Task<int> ProcessRemindersAsync(DateTime utcNow)
    {
        var today = utcNow.Date;

        // Recurring/retainer invoices (ScheduleId != null) are collected automatically by Direct
        // Debit, so they're never chased — only one-off invoices get reminders.
        var candidates = await db.Invoices
            .Include(i => i.Project)
                .ThenInclude(p => p!.Client)
            .Where(i => i.ScheduleId == null
                        && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
                        && i.DueAt != null
                        && i.ReminderStage < (int)InvoiceReminderKind.Final)
            .ToListAsync();

        var sent = 0;
        foreach (var invoice in candidates)
        {
            var kind = NextReminder(invoice, today);
            if (kind is null) continue;

            // Persist the stage (and any overdue flip) before sending so a crash or
            // send failure skips one email rather than repeating it every run.
            invoice.ReminderStage  = (int)kind.Value;
            invoice.LastReminderAt = utcNow;

            var becameOverdue = kind.Value is InvoiceReminderKind.Overdue or InvoiceReminderKind.Final
                                && invoice.Status != InvoiceStatus.Overdue;
            if (becameOverdue) invoice.Status = InvoiceStatus.Overdue;

            await db.SaveChangesAsync();

            if (becameOverdue)
                await timeline.RecordAsync(
                    invoice.ProjectId, TimelineEventType.InvoiceOverdue,
                    $"Invoice overdue: {invoice.Title}",
                    TimelineVisibility.ClientVisible, actorId: null, occurredAt: utcNow);

            if (await TrySendAsync(invoice, kind.Value)) sent++;
        }

        return sent;
    }

    /// <summary>
    /// The reminder now due for this invoice, or null. Skips straight to the latest
    /// applicable stage (e.g. an invoice already a week overdue when first seen gets
    /// one final reminder, not three emails at once).
    /// </summary>
    internal static InvoiceReminderKind? NextReminder(Invoice invoice, DateTime today)
    {
        if (invoice.DueAt is not { } dueAt) return null;
        var due = dueAt.Date;

        if (today >= due.AddDays(FinalDelayDays) && invoice.ReminderStage < (int)InvoiceReminderKind.Final)
            return InvoiceReminderKind.Final;

        if (today > due && invoice.ReminderStage < (int)InvoiceReminderKind.Overdue)
            return InvoiceReminderKind.Overdue;

        if (today >= due.AddDays(-UpcomingLeadDays) && today <= due
            && invoice.ReminderStage < (int)InvoiceReminderKind.Upcoming)
            return InvoiceReminderKind.Upcoming;

        return null;
    }

    /// <summary>Sends the reminder email. Never throws — a send failure is logged and skipped.</summary>
    private async Task<bool> TrySendAsync(Invoice invoice, InvoiceReminderKind kind)
    {
        try
        {
            if (invoice.Project?.Client?.Email is not { Length: > 0 }) return false;
            var url = siteOptions.Value.PortalProjectUrl(invoice.ProjectId);
            await email.SendInvoiceReminderAsync(invoice.Project.Client, invoice.Project, invoice, url, kind);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send {Kind} reminder for invoice {InvoiceId}", kind, invoice.Id);
            return false;
        }
    }
}
