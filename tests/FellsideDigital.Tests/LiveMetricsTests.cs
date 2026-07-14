using FellsideDigital.Web.Services;
using FellsideDigital.Web.Services.Live;

namespace FellsideDigital.Tests;

public class LiveMetricsTests
{
    private static LiveParticipant P(
        string name, string? company = null, string device = "iOS",
        string? domain = "gmail.com", int secondsOffset = 0) =>
        new(name, company, new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero).AddSeconds(secondsOffset),
            device, domain);

    [Fact]
    public void Empty_input_returns_empty_metrics()
    {
        var m = LiveMetricsBuilder.Build([]);
        Assert.Equal(0, m.Total);
        Assert.Empty(m.Devices);
        Assert.Empty(m.Sources);
    }

    [Fact]
    public void Counts_total_and_device_split()
    {
        var m = LiveMetricsBuilder.Build([
            P("a", device: "iOS"),
            P("b", device: "iOS"),
            P("c", device: "Android"),
            P("d", device: "Desktop"),
        ]);

        Assert.Equal(4, m.Total);
        Assert.Equal("iOS", m.Devices[0].Label);
        Assert.Equal(2, m.Devices[0].Count);
        Assert.Equal(3, m.Devices.Count);
    }

    [Fact]
    public void Sources_are_companies_when_at_least_two_distinct()
    {
        var m = LiveMetricsBuilder.Build([
            P("a", company: "Acme"),
            P("b", company: "Acme"),
            P("c", company: "Globex"),
        ]);

        Assert.True(m.SourcesAreCompanies);
        Assert.Equal("Acme", m.Sources[0].Label);
        Assert.Equal(2, m.Sources[0].Count);
    }

    [Fact]
    public void Sources_fall_back_to_providers_when_companies_sparse()
    {
        // All consumer email, no resolvable company → provider breakdown.
        var m = LiveMetricsBuilder.Build([
            P("a", domain: "gmail.com"),
            P("b", domain: "gmail.com"),
            P("c", domain: "outlook.com"),
            P("d", domain: "icloud.com"),
        ]);

        Assert.False(m.SourcesAreCompanies);
        Assert.Equal("Gmail", m.Sources[0].Label);
        Assert.Equal(2, m.Sources[0].Count);
        Assert.Contains(m.Sources, s => s.Label == "Outlook");
        Assert.Contains(m.Sources, s => s.Label == "iCloud");
    }

    [Fact]
    public void Timeline_buckets_sum_to_total_and_expose_peak()
    {
        var m = LiveMetricsBuilder.Build([
            P("a", secondsOffset: 0),
            P("b", secondsOffset: 1),
            P("c", secondsOffset: 2),
            P("d", secondsOffset: 60),
        ], timelineBuckets: 4);

        Assert.Equal(4, m.Timeline.Count);
        Assert.Equal(4, m.Timeline.Sum());
        Assert.Equal(3, m.PeakCount); // first three land in the opening bucket
    }

    [Fact]
    public void Simultaneous_joins_collapse_into_one_burst()
    {
        var m = LiveMetricsBuilder.Build([
            P("a", secondsOffset: 0),
            P("b", secondsOffset: 0),
        ], timelineBuckets: 5);

        Assert.Equal(2, m.Timeline.Sum());
        Assert.Equal(2, m.PeakCount);
        Assert.Equal(TimeSpan.Zero, m.Duration);
    }
}
