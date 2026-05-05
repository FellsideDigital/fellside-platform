using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CompanyName { get; set; }
        public string? ServiceType { get; set; }
        public string? ProjectDescription { get; set; }
        public string? Notes { get; set; }
        public Guid? InvitationId { get; set; }
        public ClientInvitation? Invitation { get; set; }
    }
}
