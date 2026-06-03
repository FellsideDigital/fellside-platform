using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class QrLeadService(FellsideDigitalDbContext db) : IQrLeadService
{
    private static readonly HashSet<string> ValidScanSources =
        new(StringComparer.OrdinalIgnoreCase) { "shirt", "card" };

    public async Task<QrScan> RecordScanAsync(string source, string? ipAddress, string? userAgent)
    {
        var scan = new QrScan
        {
            Source = ValidScanSources.Contains(source) ? source.ToLowerInvariant() : "unknown",
            IpAddress = ipAddress,
            UserAgent = userAgent,
        };

        db.QrScans.Add(scan);
        await db.SaveChangesAsync();
        return scan;
    }

    public async Task<QrLead> CreateLeadAsync(QrLead lead)
    {
        db.QrLeads.Add(lead);
        await db.SaveChangesAsync();
        return lead;
    }

    public async Task<List<QrLead>> GetLeadsAsync()
        => await db.QrLeads
            .OrderByDescending(l => l.SubmittedAt)
            .ToListAsync();

    public async Task<QrCampaignStats> GetCampaignStatsAsync()
    {
        var scans = await db.QrScans.ToListAsync();
        var totalLeads = await db.QrLeads.CountAsync();

        return new QrCampaignStats
        {
            TotalScans = scans.Count,
            ShirtScans = scans.Count(s => s.Source == "shirt"),
            CardScans = scans.Count(s => s.Source == "card"),
            TotalLeads = totalLeads,
        };
    }

    public async Task MarkLeadAsReadAsync(Guid id)
    {
        var entity = await db.QrLeads.FindAsync(id);
        if (entity is null || entity.IsRead) return;

        entity.IsRead = true;
        await db.SaveChangesAsync();
    }
}
