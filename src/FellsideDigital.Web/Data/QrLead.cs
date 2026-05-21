namespace FellsideDigital.Web.Data;

public class QrLead
{
    public Guid Id { get; set; }
    public string Source { get; set; } = "";       // "shirt" | "card"
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string Interest { get; set; } = "";     // "Website design" | "Automation" | "Both" | "Not sure yet"
    public string? Budget { get; set; }            // "Under £1k" | "£1k–£3k" | "£3k–£10k" | "£10k+"
    public string? Timeline { get; set; }          // "ASAP" | "1–3 months" | "3–6 months" | "Just exploring"
    public string? Message { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public Guid? QrScanId { get; set; }
    public QrScan? QrScan { get; set; }
}
