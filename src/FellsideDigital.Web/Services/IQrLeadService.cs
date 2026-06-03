using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IQrLeadService
{
    /// <summary>Records a QR scan, normalising the source to a known value, and returns the persisted scan.</summary>
    Task<QrScan> RecordScanAsync(string source, string? ipAddress, string? userAgent);

    Task<QrLead> CreateLeadAsync(QrLead lead);
    Task<List<QrLead>> GetLeadsAsync();
    Task<QrCampaignStats> GetCampaignStatsAsync();
    Task MarkLeadAsReadAsync(Guid id);
}

public sealed record QrCampaignStats
{
    public int TotalScans { get; init; }
    public int ShirtScans { get; init; }
    public int CardScans { get; init; }
    public int TotalLeads { get; init; }
}
