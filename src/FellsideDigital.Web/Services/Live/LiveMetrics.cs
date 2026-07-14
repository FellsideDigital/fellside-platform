namespace FellsideDigital.Web.Services.Live;

/// <summary>A single labelled tally used by the results charts.</summary>
public record MetricSlice(string Label, int Count);

/// <summary>
/// Snapshot of aggregate stats for a finished live session, built from the
/// full participant list. Pure/deterministic so it can be unit-tested without
/// a circuit or database.
/// </summary>
public record LiveMetrics(
    int Total,
    IReadOnlyList<MetricSlice> Devices,
    IReadOnlyList<MetricSlice> Sources,
    bool SourcesAreCompanies,
    IReadOnlyList<int> Timeline,
    int PeakCount,
    string PeakWindowLabel,
    TimeSpan Duration)
{
    public static readonly LiveMetrics Empty = new(
        0, [], [], false, [], 0, "", TimeSpan.Zero);
}

public static class LiveMetricsBuilder
{
    // Minimum distinct resolved companies before the "sources" chart shows
    // companies instead of the (more likely) email-provider breakdown.
    private const int CompanyThreshold = 2;

    public static LiveMetrics Build(IReadOnlyList<LiveParticipant> participants, int timelineBuckets = 12)
    {
        if (participants.Count == 0) return LiveMetrics.Empty;
        if (timelineBuckets < 1) timelineBuckets = 1;

        var devices = participants
            .GroupBy(p => string.IsNullOrWhiteSpace(p.DeviceType) ? DeviceDetector.Other : p.DeviceType)
            .Select(g => new MetricSlice(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Label, StringComparer.Ordinal)
            .ToList();

        var (sources, sourcesAreCompanies) = BuildSources(participants);
        var (timeline, peak, peakLabel, duration) = BuildTimeline(participants, timelineBuckets);

        return new LiveMetrics(
            participants.Count, devices, sources, sourcesAreCompanies,
            timeline, peak, peakLabel, duration);
    }

    private static (IReadOnlyList<MetricSlice>, bool) BuildSources(IReadOnlyList<LiveParticipant> participants)
    {
        var companies = participants
            .Where(p => !string.IsNullOrWhiteSpace(p.Company))
            .GroupBy(p => p.Company!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MetricSlice(g.First().Company!, g.Count()))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        if (companies.Count >= CompanyThreshold) return (companies, true);

        var providers = participants
            .Select(p => ProviderLabel(p.Domain))
            .Where(l => l is not null)
            .GroupBy(l => l!, StringComparer.OrdinalIgnoreCase)
            .Select(g => new MetricSlice(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ThenBy(s => s.Label, StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        return (providers, false);
    }

    private static (IReadOnlyList<int>, int, string, TimeSpan) BuildTimeline(
        IReadOnlyList<LiveParticipant> participants, int buckets)
    {
        var times = participants.Select(p => p.JoinedAt).OrderBy(t => t).ToList();
        var first = times[0];
        var last = times[^1];
        var duration = last - first;

        var counts = new int[buckets];
        if (duration <= TimeSpan.Zero)
        {
            counts[0] = times.Count; // everyone in one burst
        }
        else
        {
            foreach (var t in times)
            {
                var frac = (t - first).TotalMilliseconds / duration.TotalMilliseconds;
                var idx = (int)(frac * buckets);
                if (idx >= buckets) idx = buckets - 1;
                counts[idx]++;
            }
        }

        var peak = counts.Max();
        var bucketSeconds = duration <= TimeSpan.Zero
            ? 0
            : Math.Max(1, (int)Math.Round(duration.TotalSeconds / buckets));
        var peakLabel = peak <= 1
            ? "steady stream"
            : bucketSeconds == 0
                ? $"{peak} joined in one burst"
                : $"{peak} joined in a {bucketSeconds}s burst";

        return (counts, peak, peakLabel, duration);
    }

    private static string? ProviderLabel(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return null;
        var d = domain.Trim().ToLowerInvariant();

        return d switch
        {
            "gmail.com" or "googlemail.com" => "Gmail",
            "outlook.com" or "hotmail.com" or "hotmail.co.uk" or "live.com" or "msn.com" => "Outlook",
            "yahoo.com" or "yahoo.co.uk" or "ymail.com" => "Yahoo",
            "icloud.com" or "me.com" or "mac.com" => "iCloud",
            "protonmail.com" or "proton.me" or "pm.me" => "Proton",
            "aol.com" => "AOL",
            _ => d, // a real business domain — show it as-is
        };
    }
}
