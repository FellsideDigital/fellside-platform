using FellsideDigital.Tests.TestSupport;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

[Collection(PostgresCollection.Name)]
public class QrLeadServiceTests(PostgresFixture fx)
{
    [Theory]
    [InlineData("shirt", "shirt")]
    [InlineData("CARD", "card")]
    [InlineData("anything-else", "unknown")]
    public async Task RecordScanAsync_normalises_the_source(string input, string expected)
    {
        await using var db = fx.CreateContext();
        var sut = new QrLeadService(db);

        var scan = await sut.RecordScanAsync(input, "127.0.0.1", "agent");

        Assert.Equal(expected, scan.Source);
        Assert.NotEqual(Guid.Empty, scan.Id);
    }

    [Fact]
    public async Task CreateLeadAsync_persists_the_lead()
    {
        await using var db = fx.CreateContext();
        var sut = new QrLeadService(db);

        var lead = await sut.CreateLeadAsync(new QrLead
        {
            Source = "shirt",
            Name = "Lead",
            Email = "lead@example.com",
            Interest = "Website design",
        });

        await using var verify = fx.CreateContext();
        var found = await verify.QrLeads.FindAsync(lead.Id);
        Assert.NotNull(found);
        Assert.False(found!.IsRead);
    }

    [Fact]
    public async Task MarkLeadAsReadAsync_sets_the_read_flag()
    {
        await using var db = fx.CreateContext();
        var sut = new QrLeadService(db);
        var lead = await sut.CreateLeadAsync(new QrLead
        {
            Source = "card", Name = "X", Email = "x@e.com", Interest = "Automation",
        });

        await sut.MarkLeadAsReadAsync(lead.Id);

        await using var verify = fx.CreateContext();
        var found = await verify.QrLeads.FindAsync(lead.Id);
        Assert.True(found!.IsRead);
    }

    [Fact]
    public async Task GetCampaignStatsAsync_reflects_recorded_scans_and_leads()
    {
        await using var db = fx.CreateContext();
        var sut = new QrLeadService(db);
        await sut.RecordScanAsync("shirt", null, null);
        await sut.RecordScanAsync("card", null, null);
        await sut.CreateLeadAsync(new QrLead { Source = "shirt", Name = "S", Email = "s@e.com", Interest = "Both" });

        var stats = await sut.GetCampaignStatsAsync();

        Assert.True(stats.TotalScans >= 2);
        Assert.True(stats.ShirtScans >= 1);
        Assert.True(stats.CardScans >= 1);
        Assert.True(stats.TotalLeads >= 1);
    }
}
