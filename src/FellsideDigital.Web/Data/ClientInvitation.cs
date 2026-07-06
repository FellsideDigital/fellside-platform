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

    /// <summary>The client's job title (e.g. "Director"), used for testimonial attribution.</summary>
    public string JobTitle { get; set; } = "";

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

    /// <summary>When set, accepting this invitation attaches the new user to this
    /// project (as primary if <see cref="IsPrimaryClient"/>, else as a collaborator).
    /// Null for a standalone account-only invitation.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>Only meaningful when <see cref="ProjectId"/> is set: attach the new
    /// user as the project's primary client (if it has none yet) rather than a member.</summary>
    public bool IsPrimaryClient { get; set; }
}
