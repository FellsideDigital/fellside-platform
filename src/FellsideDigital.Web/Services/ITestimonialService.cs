using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface ITestimonialService
{
    /// <summary>Approved testimonials for the public site, newest-approved first.</summary>
    Task<List<ClientTestimonial>> GetApprovedAsync();

    /// <summary>The given client's testimonial, or null if they haven't written one.</summary>
    Task<ClientTestimonial?> GetForUserAsync(string userId);

    /// <summary>
    /// Creates or updates the client's single testimonial and (re)sets it to
    /// <see cref="Domain.Enums.TestimonialStatus.Pending"/>. Throws
    /// <see cref="InvalidOperationException"/> with a user-facing message for invalid input.
    /// </summary>
    Task SubmitOrUpdateAsync(string userId, int rating, string quote, string authorName, string authorRole);

    /// <summary>All testimonials, newest first (admin moderation).</summary>
    Task<List<ClientTestimonial>> GetAllAsync();

    /// <summary>Approves or rejects a testimonial; stamps <c>ApprovedAt</c> when approving.</summary>
    Task SetStatusAsync(Guid id, Domain.Enums.TestimonialStatus status);

    Task DeleteAsync(Guid id);
}
