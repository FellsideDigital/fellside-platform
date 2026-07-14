using System.Security.Cryptography;
using System.Text;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services.Live;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class VisitorAnalyticsService(
    FellsideDigitalDbContext db,
    IConfiguration config,
    ILogger<VisitorAnalyticsService> logger) : IVisitorAnalyticsService
{
    // Salt for the one-way IP hash. Configured value preferred; the fallback keeps the
    // hash non-trivial in dev. The raw IP is never persisted.
    private string IpHashSalt => config["Analytics:IpHashSalt"] ?? "fellside-analytics-v1";

    public async Task RecordAsync(VisitorCapture capture, VisitorRequestContext context, CancellationToken ct = default)
    {
        var evt = new VisitorEvent
        {
            SessionId = capture.SessionId,
            Path = Trim(capture.Path, 512) ?? "/",
            Platform = DeviceDetector.Classify(context.UserAgent),
            Browser = BrowserDetector.Classify(context.UserAgent),
            IpHash = HashIp(context.IpAddress),
            Country = Trim(context.Country, 8),
            Language = Trim(capture.Language, 32),
            Timezone = Trim(capture.Timezone, 64),
            ScreenWidth = Clamp(capture.ScreenWidth),
            ScreenHeight = Clamp(capture.ScreenHeight),
            ViewportWidth = Clamp(capture.ViewportWidth),
            ViewportHeight = Clamp(capture.ViewportHeight),
            Referrer = NormalizeReferrer(capture.Referrer),
            UtmSource = Trim(capture.UtmSource, 128),
            UtmMedium = Trim(capture.UtmMedium, 128),
            UtmCampaign = Trim(capture.UtmCampaign, 128),
            EngagementSeconds = Clamp(capture.EngagementSeconds, 0, 86_400),
            ScrollDepthPercent = Clamp(capture.ScrollDepthPercent, 0, 100),
        };

        db.Set<VisitorEvent>().Add(evt);
        await db.SaveChangesAsync(ct);
    }

    public async Task<VisitorAnalyticsSummary> GetSummaryAsync(int days = 30, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 365);
        var since = DateTime.UtcNow.AddDays(-days);

        var events = await db.Set<VisitorEvent>()
            .AsNoTracking()
            .Where(e => e.OccurredAt >= since)
            .ToListAsync(ct);

        if (events.Count == 0)
            return new VisitorAnalyticsSummary { RangeDays = days };

        var engagement = events.Where(e => e.EngagementSeconds.HasValue).Select(e => e.EngagementSeconds!.Value).ToList();

        return new VisitorAnalyticsSummary
        {
            RangeDays = days,
            TotalVisits = events.Count,
            UniqueVisitors = events.Select(e => e.SessionId).Distinct().Count(),
            AvgEngagementSeconds = engagement.Count > 0 ? Math.Round(engagement.Average(), 1) : 0,
            Platforms = TopBy(events, e => e.Platform),
            Browsers = TopBy(events, e => e.Browser),
            Countries = TopBy(events, e => e.Country ?? "Unknown"),
            Referrers = TopBy(events, e => string.IsNullOrWhiteSpace(e.Referrer) ? "Direct" : e.Referrer!),
            Campaigns = TopBy(events.Where(e => !string.IsNullOrWhiteSpace(e.UtmCampaign)),
                              e => e.UtmCampaign!),
            TopPages = TopBy(events, e => e.Path),
        };
    }

    private static List<CountRow> TopBy(IEnumerable<VisitorEvent> events, Func<VisitorEvent, string> key, int take = 8)
        => events.GroupBy(key)
            .Select(g => new CountRow(g.Key, g.Count()))
            .OrderByDescending(r => r.Count)
            .Take(take)
            .ToList();

    private string? HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        try
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(IpHashSalt + ip));
            return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
        }
        catch (Exception ex)
        {
            // Never let hashing failure block a request; just drop the (optional) hash.
            logger.LogWarning(ex, "Failed to hash visitor IP");
            return null;
        }
    }

    private static string? NormalizeReferrer(string? referrer)
    {
        if (string.IsNullOrWhiteSpace(referrer)) return null;
        // Store the host only — never the full URL (avoids capturing querystrings/PII).
        return Uri.TryCreate(referrer, UriKind.Absolute, out var uri) ? uri.Host : Trim(referrer, 128);
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        value = value.Trim();
        return value.Length <= max ? value : value[..max];
    }

    private static int? Clamp(int? value, int min = 0, int max = 20_000)
        => value is null ? null : Math.Clamp(value.Value, min, max);
}
