using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class TestimonialService(FellsideDigitalDbContext db) : ITestimonialService
{
    public const int MaxQuoteLength = 1000;

    public Task<List<ClientTestimonial>> GetApprovedAsync() =>
        db.ClientTestimonials
            .AsNoTracking()
            .Where(t => t.Status == TestimonialStatus.Approved)
            .OrderByDescending(t => t.ApprovedAt)
            .ToListAsync();

    public Task<ClientTestimonial?> GetForUserAsync(string userId) =>
        db.ClientTestimonials.FirstOrDefaultAsync(t => t.UserId == userId);

    public async Task SubmitOrUpdateAsync(string userId, int rating, string quote, string authorName, string authorRole)
    {
        if (rating is < 1 or > 5)
            throw new InvalidOperationException("Please choose a rating between 1 and 5 stars.");

        quote = (quote ?? "").Trim();
        if (quote.Length == 0)
            throw new InvalidOperationException("Please write a few words for your testimonial.");
        if (quote.Length > MaxQuoteLength)
            throw new InvalidOperationException($"Your testimonial is too long — please keep it under {MaxQuoteLength} characters.");

        var now = DateTime.UtcNow;
        var existing = await db.ClientTestimonials.FirstOrDefaultAsync(t => t.UserId == userId);

        if (existing is null)
        {
            db.ClientTestimonials.Add(new ClientTestimonial
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Rating = rating,
                Quote = quote,
                AuthorName = (authorName ?? "").Trim(),
                AuthorRole = (authorRole ?? "").Trim(),
                Status = TestimonialStatus.Pending,
                SubmittedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.Rating = rating;
            existing.Quote = quote;
            existing.AuthorName = (authorName ?? "").Trim();
            existing.AuthorRole = (authorRole ?? "").Trim();
            existing.Status = TestimonialStatus.Pending;
            existing.ApprovedAt = null;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync();
    }

    public Task<List<ClientTestimonial>> GetAllAsync() =>
        db.ClientTestimonials
            .AsNoTracking()
            .OrderByDescending(t => t.SubmittedAt)
            .ToListAsync();

    public async Task SetStatusAsync(Guid id, TestimonialStatus status)
    {
        var testimonial = await db.ClientTestimonials.FindAsync(id);
        if (testimonial is null) return;

        testimonial.Status = status;
        testimonial.ApprovedAt = status == TestimonialStatus.Approved ? DateTime.UtcNow : null;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var testimonial = await db.ClientTestimonials.FindAsync(id);
        if (testimonial is null) return;

        db.ClientTestimonials.Remove(testimonial);
        await db.SaveChangesAsync();
    }
}
