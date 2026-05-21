namespace FellsideDigital.Web.Data;

public class QrScan
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";   // "shirt" | "card"
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public ICollection<QrLead> Leads { get; set; } = [];
}
