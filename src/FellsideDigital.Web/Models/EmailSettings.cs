using System.ComponentModel.DataAnnotations;

namespace FellsideDigital.Web.Models;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.office365.com";
    public int SmtpPort { get; set; } = 587;

    [Required(ErrorMessage = "Email:ClientId is required (Azure AD app registration client ID)")]
    public string ClientId { get; set; } = "";

    [Required(ErrorMessage = "Email:TenantId is required (Azure AD tenant ID)")]
    public string TenantId { get; set; } = "";

    [Required(ErrorMessage = "Email:ClientSecret is required (Azure AD app registration secret)")]
    public string ClientSecret { get; set; } = "";

    public string FromName { get; set; } = "Fellside Digital";

    [Required(ErrorMessage = "Email:FromAddress is required (licensed Exchange mailbox e.g. hello@fellsidedigital.co.uk)")]
    public string FromAddress { get; set; } = "";

    [Required(ErrorMessage = "Email:AdminEmail is required")]
    public string AdminEmail { get; set; } = "";
}
