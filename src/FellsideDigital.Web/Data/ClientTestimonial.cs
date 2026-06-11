using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Data;

/// <summary>
/// A testimonial submitted by a client. One per client (see the unique index on
/// <see cref="UserId"/>). Held as <see cref="TestimonialStatus.Pending"/> until a
/// SiteAdmin approves it; only approved testimonials surface on the public site.
/// </summary>
public class ClientTestimonial
{
    public Guid Id { get; set; }

    /// <summary>The client who wrote it. Unique — one testimonial per client.</summary>
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }

    /// <summary>1–5 stars.</summary>
    public int Rating { get; set; }

    public string Quote { get; set; } = "";

    /// <summary>Display name snapshotted at submit time so public cards stay stable.</summary>
    public string AuthorName { get; set; } = "";

    /// <summary>Role/company line (e.g. "Director, Acme Ltd"), snapshotted at submit time.</summary>
    public string AuthorRole { get; set; } = "";

    public TestimonialStatus Status { get; set; } = TestimonialStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
}
