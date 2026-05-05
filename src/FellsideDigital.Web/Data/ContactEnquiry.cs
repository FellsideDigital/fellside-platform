namespace FellsideDigital.Web.Data;

public class ContactEnquiry
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public string? Company { get; set; }
    public string ServiceType { get; set; } = "";
    public string? Budget { get; set; }
    public string Message { get; set; } = "";
    public string? HowHeard { get; set; }
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
}
