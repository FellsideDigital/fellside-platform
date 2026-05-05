using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

public class ClientInvitation
{
    public Guid Id { get; set; }

    /// <summary>32-byte crypto-random token, URL-safe base64 encoded.</summary>
    public string Token { get; set; } = "";

    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string ServiceType { get; set; } = "";
    public string ProjectDescription { get; set; } = "";

    /// <summary>Internal admin notes — never shown to the client.</summary>
    public string Notes { get; set; } = "";

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string CreatedByUserId { get; set; } = "";
    public ApplicationUser? CreatedBy { get; set; }

    public DateTime? AcceptedAt { get; set; }
    public string? AcceptedUserId { get; set; }
    public ApplicationUser? AcceptedUser { get; set; }
}
