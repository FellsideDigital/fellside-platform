using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services;

public interface IEnquiryService
{
    Task<ContactEnquiry> CreateAsync(ContactEnquiry enquiry);
    Task<List<ContactEnquiry>> GetAllAsync();
    Task MarkAsReadAsync(Guid id);
}
