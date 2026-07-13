using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Services;

/// <summary>
/// The single email abstraction for the app. Covers Identity's built-in flows
/// (via IEmailSender&lt;ApplicationUser&gt;) and every transactional email we send,
/// so all mail runs through one pipeline (EmailService → Microsoft Graph).
/// </summary>
public interface IEmailService : IEmailSender<ApplicationUser>
{
    Task SendInvitationAsync(ClientInvitation invitation, string registrationUrl);
    Task SendClientRegisteredNotificationAsync(ApplicationUser user);
    Task SendWelcomeEmailAsync(ApplicationUser user);
    Task SendContactEnquiryAsync(ContactEnquiry enquiry);
    Task SendQrLeadNotificationAsync(QrLead lead);
    Task SendQrLeadDiscountAsync(QrLead lead);
    Task SendLiveAutomationWelcomeAsync(QrLead lead);
    Task SendDocumentAddedAsync(ApplicationUser client, ClientProject project, string documentTitle, string portalUrl);
    Task SendInvoiceAddedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendInvoiceUpdatedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendInvoiceStatusChangedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl);
    Task SendTestimonialRequestAsync(ApplicationUser client, ClientProject project, string testimonialUrl);
}
