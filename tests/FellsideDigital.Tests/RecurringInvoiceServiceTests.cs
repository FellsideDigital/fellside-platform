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
public class RecurringInvoiceServiceTests(PostgresFixture fx)
{
    private static (RecurringInvoiceService Sut, FakeEmailService Email) MakeSut(FellsideDigitalDbContext db)
    {
        var email = new FakeEmailService();
        var sut = new RecurringInvoiceService(
            db, new ProjectTimelineService(db), email,
            Options.Create(new SiteSettings()),
            NullLogger<RecurringInvoiceService>.Instance);
        return (sut, email);
    }

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
    public async Task Generate_IssuesInvoice_AdvancesSchedule_AndEmailsClient()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var (sut, email) = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Hosting retainer", "Monthly hosting", 99.50m, "GBP", dayOfMonth: 15, dueDays: 14);
        var period = schedule.NextIssueDate;
        var runAt = period.AddHours(9);

        var issued = await sut.GenerateDueInvoicesAsync(runAt);

        Assert.Equal(1, issued);
        var invoice = await db.Invoices.SingleAsync(i => i.ScheduleId == schedule.Id);
        Assert.Equal($"Hosting retainer — {period:MMMM yyyy}", invoice.Title);
        Assert.Equal(99.50m, invoice.Amount);
        Assert.Equal(InvoiceStatus.Sent, invoice.Status);
        Assert.Equal(invoice.IssuedAt.Date.AddDays(14), invoice.DueAt!.Value.Date);
        Assert.Contains(invoice.Id, email.InvoiceAdded);

        var updated = await db.RecurringInvoiceSchedules.SingleAsync(s => s.Id == schedule.Id);
        Assert.True(updated.NextIssueDate.Date > runAt.Date);
        Assert.NotNull(updated.LastIssuedAt);
    }

    [Fact]
    public async Task Generate_IsIdempotent_WithinTheSameDay()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var (sut, _) = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 200m, "GBP", 1, 14);
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
        var (sut, _) = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Paused retainer", null, 50m, "GBP", 1, 7);
        await sut.SetActiveAsync(schedule.Id, false);

        Assert.Equal(0, await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddDays(1)));
    }

    [Fact]
    public async Task Generate_BackIssuesOneInvoicePerMissedMonth()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var (sut, _) = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 100m, "GBP", 1, 14);

        // Two months and a bit of "downtime" after the first issue date.
        var issued = await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddMonths(2).AddDays(3));

        Assert.Equal(3, issued);
        Assert.Equal(3, await db.Invoices.CountAsync(i => i.ScheduleId == schedule.Id));
    }

    [Fact]
    public async Task Generate_WithoutClient_StillIssuesInvoice_WithoutEmail()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db, withClient: false);
        var (sut, email) = MakeSut(db);

        var schedule = await sut.CreateAsync(projectId, "Retainer", null, 75m, "GBP", 1, 14);
        var issued = await sut.GenerateDueInvoicesAsync(schedule.NextIssueDate.AddHours(9));

        Assert.Equal(1, issued);
        Assert.Empty(email.InvoiceAdded);
    }

    [Fact]
    public async Task Create_RejectsInvalidInput()
    {
        await using var db = fx.CreateContext();
        var projectId = await SeedProjectAsync(db);
        var (sut, _) = MakeSut(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "", null, 10m, "GBP", 1, 14));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "T", null, 0m, "GBP", 1, 14));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "T", null, 10m, "GBP", 32, 14));
        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(projectId, "T", null, 10m, "GBP", 1, 91));
    }
}
