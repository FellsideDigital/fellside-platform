using FellsideDigital.Domain.Enums;
using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using FellsideDigital.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class RecurringInvoiceServiceTests(PostgresFixture fx)
{
    private const int PaymentDay = 1;

    private static RecurringInvoiceService MakeSut(FellsideDigitalDbContext db)
        => new(db, new ProjectTimelineService(db),
            Options.Create(new BillingSettings { PaymentDayOfMonth = PaymentDay }));

    private static async Task<Guid> SeedProjectAsync(FellsideDigitalDbContext db, bool withClient = true)
    {
        var admin = new ApplicationUser { UserName = $"a{Guid.NewGuid():N}@x.io", Email = "a@x.io" };
        db.Users.Add(admin);

        ApplicationUser? client = null;
        if (withClient)
        {
            client = new ApplicationUser { UserName = $"c{Guid.NewGuid():N}@x.io", Email = "c@x.io" };
            db.Users.Add(client);
        }

        var project = new ClientProject
        {
            Name = "Retainer project", Description = "D",
            Status = ProjectStatus.InProgress, Type = ProjectType.Website,
            ClientId = client?.Id, CreatedByAdminId = admin.Id,
        };
        db.ClientProjects.Add(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    [Fact]
    public async Task Create_AnchorsNextIssue_ToGlobalPaymentDay()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP");

        Assert.Equal(PaymentDay, schedule.NextIssueDate.Day);
        Assert.True(schedule.NextIssueDate.Date >= DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Generate_IssuesInvoice_DueOnCollectionDay_AdvancesSchedule()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Hosting retainer", "Monthly hosting", 99.50m, "GBP");
        var period = schedule.NextIssueDate;
        var runAt = period.AddHours(9);

        var issued = await sut.GenerateDueInvoicesAsync(runAt);

        Assert.Equal(1, issued);
        var invoice = await db.Invoices.SingleAsync(i => i.ScheduleId == schedule.Id);
        Assert.Equal($"Hosting retainer — {period:MMMM yyyy}", invoice.Title);
        Assert.Equal(99.50m, invoice.Amount);
        Assert.Equal(InvoiceStatus.Sent, invoice.Status);

        // Payment is collected on the issue day, so the due date is that same day. The retainer
        // is portal-only (no email), and ReminderStage is seeded past "due soon" so the reminder
        // worker — which skips schedule-generated invoices anyway — never treats it as a nudge.
        Assert.Equal(period.Date, invoice.DueAt!.Value.Date);
        Assert.Equal((int)InvoiceReminderKind.Upcoming, invoice.ReminderStage);

        var updated = await db.RecurringInvoiceSchedules.SingleAsync(s => s.Id == schedule.Id);
        Assert.True(updated.NextIssueDate.Date > runAt.Date);
        Assert.Equal(PaymentDay, updated.NextIssueDate.Day);
        Assert.NotNull(updated.LastIssuedAt);
    }

    [Fact]
    public async Task Generate_IsIdempotent_WithinTheSameDay()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 200m, "GBP");
        var runAt = schedule.NextIssueDate.AddHours(9);

        Assert.Equal(1, await sut.GenerateDueInvoicesAsync(runAt));
        Assert.Equal(0, await sut.GenerateDueInvoicesAsync(runAt));
        Assert.Equal(0, await sut.GenerateDueInvoicesAsync(runAt.AddHours(5)));
    }

    [Fact]
    public async Task Generate_SkipsInactiveSchedules()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Paused retainer", null, 50m, "GBP");
        await sut.SetActiveAsync(schedule.Id, false);

        Assert.Equal(0, await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddDays(1)));
    }

    [Fact]
    public async Task Generate_BackIssuesOneInvoicePerMissedMonth()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP");

        // Two months and a bit of "downtime" after the first issue date.
        var issued = await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddMonths(2).AddDays(3));

        Assert.Equal(3, issued);
        Assert.Equal(3, await db.Invoices.CountAsync(i => i.ScheduleId == schedule.Id));
    }

    [Fact]
    public async Task Generate_WithoutClient_StillIssuesInvoice()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db, withClient: false);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 75m, "GBP");
        var issued = await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddHours(9));

        Assert.Equal(1, issued);
        Assert.Equal(1, await db.Invoices.CountAsync(i => i.ScheduleId == schedule.Id));
    }

    [Fact]
    public async Task Create_RejectsInvalidInput()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "", null, 10m, "GBP"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "T", null, 0m, "GBP"));
    }

    [Fact]
    public async Task Create_WithCustomPaymentDay_AnchorsToThatDay()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", paymentDay: 18);

        Assert.Equal(18, schedule.PaymentDayOfMonth);
        Assert.Equal(18, schedule.NextIssueDate.Day);
    }

    [Fact]
    public async Task Create_DefaultsToGlobalPaymentDay_WhenNoneGiven()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP");

        Assert.Equal(PaymentDay, schedule.PaymentDayOfMonth);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public async Task Create_RejectsOutOfRangePaymentDay(int day)
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.CreateAsync(projectId, "T", null, 10m, "GBP", paymentDay: day));
    }

    [Fact]
    public async Task Update_ChangingPaymentDay_ReanchorsNextIssue()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", paymentDay: 1);
        await sut.UpdateAsync(schedule.Id, "Retainer", null, 100m, "GBP", paymentDay: 18,
            firstPaymentDate: schedule.FirstPaymentDate);

        var updated = await db.RecurringInvoiceSchedules.SingleAsync(s => s.Id == schedule.Id);
        Assert.Equal(18, updated.PaymentDayOfMonth);
        Assert.Equal(18, updated.NextIssueDate.Day);
        Assert.True(updated.NextIssueDate.Date >= DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Generate_IssuesOnScheduleOwnPaymentDay_AndAdvancesToSameDayNextMonth()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", paymentDay: 18);
        var period = schedule.NextIssueDate;
        Assert.Equal(18, period.Day);

        await sut.GenerateDueInvoicesAsync(period.AddHours(9));

        var invoice = await db.Invoices.SingleAsync(i => i.ScheduleId == schedule.Id);
        Assert.Equal(18, invoice.DueAt!.Value.Day);

        var updated = await db.RecurringInvoiceSchedules.SingleAsync(s => s.Id == schedule.Id);
        Assert.Equal(18, updated.NextIssueDate.Day);
        Assert.True(updated.NextIssueDate > period);
    }

    [Fact]
    public async Task Create_DefaultsFirstPaymentDate_ToFirstIssue()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP");

        Assert.Equal(schedule.NextIssueDate.Date, schedule.FirstPaymentDate.Date);
    }

    [Fact]
    public async Task Create_AcceptsBackdatedFirstPaymentDate()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var start = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 250m, "GBP", firstPaymentDate: start);

        var stored = await db.RecurringInvoiceSchedules.SingleAsync(s => s.Id == schedule.Id);
        Assert.Equal(start, stored.FirstPaymentDate);
    }

    [Fact]
    public async Task CollectedToDate_IsMonthsElapsed_TimesAmount()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var start = DateTime.UtcNow.Date.AddMonths(-3); // 4 payments so far (start + 3 anniversaries)
        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", firstPaymentDate: start);

        Assert.Equal(400m, sut.CollectedToDate(schedule, DateTime.UtcNow));
    }

    [Fact]
    public async Task CollectedToDate_IsZero_BeforeFirstPayment()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var sut = MakeSut(db);

        var start = DateTime.UtcNow.Date.AddMonths(2); // starts in the future
        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", firstPaymentDate: start);

        Assert.Equal(0m, sut.CollectedToDate(schedule, DateTime.UtcNow));
    }
}
