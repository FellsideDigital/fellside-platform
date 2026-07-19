using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class InvoiceReminderServiceTests(PostgresFixture fx)
{
    private static (InvoiceReminderService Sut, FakeEmailService Email) MakeSut(FellsideDigitalDbContext db)
    {
        var email = new FakeEmailService();
        var sut = new InvoiceReminderService(
            db, new ProjectTimelineService(db), email,
            Options.Create(new SiteSettings()),
            NullLogger<InvoiceReminderService>.Instance);
        return (sut, email);
    }

    private static async Task<Invoice> SeedInvoiceAsync(
        FellsideDigitalDbContext db, DateTime dueAt,
        InvoiceStatus status = InvoiceStatus.Sent, int reminderStage = 0)
    {
        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        var client = new ApplicationUser { UserName = $"c{Guid.NewGuid():N}@x.io", Email = "c@x.io" };
        db.Users.AddRange(admin, client);

        var project = new ClientProject
        {
            Name = "P", Description = "D",
            Status = ProjectStatus.InProgress, Type = ProjectType.Website,
            ClientId = client.Id, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), ProjectId = project.Id,
            Title = "Test invoice", Amount = 100m, Currency = "GBP",
            Status = status, DueAt = dueAt, ReminderStage = reminderStage,
            IssuedAt = dueAt.AddDays(-14), CreatedAt = dueAt.AddDays(-14),
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private static readonly DateTime Due = new(2026, 7, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SendsUpcomingReminder_ThreeDaysBeforeDue_Once()
    {
        await using var db = fx.CreateContext();
        var invoice = await SeedInvoiceAsync(db, Due);
        var (sut, email) = MakeSut(db);

        Assert.Equal(0, await sut.ProcessRemindersAsync(Due.AddDays(-5)));
        Assert.Equal(1, await sut.ProcessRemindersAsync(Due.AddDays(-3)));
        Assert.Equal(0, await sut.ProcessRemindersAsync(Due.AddDays(-2)));

        Assert.Equal([(invoice.Id, InvoiceReminderKind.Upcoming)], email.Reminders);
        var reloaded = await db.Invoices.SingleAsync(i => i.Id == invoice.Id);
        Assert.Equal((int)InvoiceReminderKind.Upcoming, reloaded.ReminderStage);
        Assert.Equal(InvoiceStatus.Sent, reloaded.Status);
    }

    [Fact]
    public async Task FlipsToOverdue_AndSendsOverdueReminder_AfterDueDate()
    {
        await using var db = fx.CreateContext();
        var invoice = await SeedInvoiceAsync(db, Due, reminderStage: (int)InvoiceReminderKind.Upcoming);
        var (sut, email) = MakeSut(db);

        Assert.Equal(1, await sut.ProcessRemindersAsync(Due.AddDays(1)));

        var reloaded = await db.Invoices.SingleAsync(i => i.Id == invoice.Id);
        Assert.Equal(InvoiceStatus.Overdue, reloaded.Status);
        Assert.Equal((int)InvoiceReminderKind.Overdue, reloaded.ReminderStage);
        Assert.Equal([(invoice.Id, InvoiceReminderKind.Overdue)], email.Reminders);
        Assert.True(await db.ProjectTimelineEvents.AnyAsync(
            e => e.ProjectId == invoice.ProjectId && e.Type == TimelineEventType.InvoiceOverdue));
    }

    [Fact]
    public async Task LongOverdueInvoice_GetsSingleFinalReminder_NotThree()
    {
        await using var db = fx.CreateContext();
        var invoice = await SeedInvoiceAsync(db, Due);
        var (sut, email) = MakeSut(db);

        Assert.Equal(1, await sut.ProcessRemindersAsync(Due.AddDays(10)));

        Assert.Equal([(invoice.Id, InvoiceReminderKind.Final)], email.Reminders);
        var reloaded = await db.Invoices.SingleAsync(i => i.Id == invoice.Id);
        Assert.Equal(InvoiceStatus.Overdue, reloaded.Status);
        Assert.Equal((int)InvoiceReminderKind.Final, reloaded.ReminderStage);

        // Nothing further, ever — automated chasing is capped.
        Assert.Equal(0, await sut.ProcessRemindersAsync(Due.AddDays(30)));
    }

    [Fact]
    public async Task IgnoresPaidInvoices_AndInvoicesWithoutDueDate()
    {
        await using var db = fx.CreateContext();
        await SeedInvoiceAsync(db, Due, status: InvoiceStatus.Paid);
        var noDue = await SeedInvoiceAsync(db, Due);
        noDue.DueAt = null;
        await db.SaveChangesAsync();
        var (sut, email) = MakeSut(db);

        Assert.Equal(0, await sut.ProcessRemindersAsync(Due.AddDays(10)));
        Assert.Empty(email.Reminders);
    }
}
