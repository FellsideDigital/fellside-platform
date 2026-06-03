using FellsideDigital.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Services;

public class EnquiryService(FellsideDigitalDbContext db) : IEnquiryService
{
    public async Task<ContactEnquiry> CreateAsync(ContactEnquiry enquiry)
    {
        db.ContactEnquiries.Add(enquiry);
        await db.SaveChangesAsync();
        return enquiry;
    }

    public async Task<List<ContactEnquiry>> GetAllAsync()
        => await db.ContactEnquiries
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();

    public async Task MarkAsReadAsync(Guid id)
    {
        var entity = await db.ContactEnquiries.FindAsync(id);
        if (entity is null || entity.IsRead) return;

        entity.IsRead = true;
        await db.SaveChangesAsync();
    }
}
