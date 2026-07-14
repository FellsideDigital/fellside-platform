namespace FellsideDigital.Web.Services;

/// <summary>Signals collected client-side (after consent) and posted to the capture endpoint.</summary>
public sealed record VisitorCapture
{
    public Guid SessionId { get; init; }
    public string? Path { get; init; }
    public string? Language { get; init; }
    public string? Timezone { get; init; }
    public int? ScreenWidth { get; init; }
    public int? ScreenHeight { get; init; }
    public int? ViewportWidth { get; init; }
    public int? ViewportHeight { get; init; }
    public string? Referrer { get; init; }
    public string? UtmSource { get; init; }
    public string? UtmMedium { get; init; }
    public string? UtmCampaign { get; init; }
    public int? EngagementSeconds { get; init; }
    public int? ScrollDepthPercent { get; init; }
}

/// <summary>Server-derived request context the client cannot be trusted to report.</summary>
public sealed record VisitorRequestContext
{
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? Country { get; init; }
}

public interface IVisitorAnalyticsService
{
    Task RecordAsync(VisitorCapture capture, VisitorRequestContext context, CancellationToken ct = default);
    Task<VisitorAnalyticsSummary> GetSummaryAsync(int days = 30, CancellationToken ct = default);
}

public sealed record VisitorAnalyticsSummary
{
    public int RangeDays { get; init; }
    public int TotalVisits { get; init; }
    public int UniqueVisitors { get; init; }
    public double AvgEngagementSeconds { get; init; }
    public IReadOnlyList<CountRow> Platforms { get; init; } = [];
    public IReadOnlyList<CountRow> Browsers { get; init; } = [];
    public IReadOnlyList<CountRow> Countries { get; init; } = [];
    public IReadOnlyList<CountRow> Referrers { get; init; } = [];
    public IReadOnlyList<CountRow> Campaigns { get; init; } = [];
    public IReadOnlyList<CountRow> TopPages { get; init; } = [];
}

public sealed record CountRow(string Label, int Count);
