namespace FellsideDigital.Web.Models;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.office365.com";
    public int SmtpPort { get; set; } = 587;
    public string ClientId { get; set; }
    public string TenantId { get; set; }
    public string ClientSecret { get; set; }
    public string FromName { get; set; } = "Fellside Digital";
    public string FromAddress { get; set; } = "";
    public string AdminEmail { get; set; } = "";
}
