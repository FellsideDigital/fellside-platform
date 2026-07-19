using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IEmailService"/> that records invoice-related sends so
/// tests can assert on what would have been emailed. All other sends no-op.
/// </summary>
public sealed class FakeEmailService : IEmailService
{
    public List<Guid> InvoiceAdded { get; } = [];
    public List<(Guid InvoiceId, InvoiceReminderKind Kind)> Reminders { get; } = [];

    public Task SendInvoiceAddedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl)
    {
        InvoiceAdded.Add(invoice.Id);
        return Task.CompletedTask;
    }

    public Task SendInvoiceReminderAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl, InvoiceReminderKind kind)
    {
        Reminders.Add((invoice.Id, kind));
        return Task.CompletedTask;
    }

    public Task SendInvitationAsync(ClientInvitation invitation, string registrationUrl) => Task.CompletedTask;
    public Task SendClientRegisteredNotificationAsync(ApplicationUser user) => Task.CompletedTask;
    public Task SendWelcomeEmailAsync(ApplicationUser user) => Task.CompletedTask;
    public Task SendContactEnquiryAsync(ContactEnquiry enquiry) => Task.CompletedTask;
    public Task SendQrLeadNotificationAsync(QrLead lead) => Task.CompletedTask;
    public Task SendQrLeadDiscountAsync(QrLead lead) => Task.CompletedTask;
    public Task SendLiveAutomationWelcomeAsync(QrLead lead) => Task.CompletedTask;
    public Task SendDocumentAddedAsync(ApplicationUser client, ClientProject project, string documentTitle, string portalUrl) => Task.CompletedTask;
    public Task SendInvoiceUpdatedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl) => Task.CompletedTask;
    public Task SendInvoiceStatusChangedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl) => Task.CompletedTask;
    public Task SendTestimonialRequestAsync(ApplicationUser client, ClientProject project, string testimonialUrl) => Task.CompletedTask;
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) => Task.CompletedTask;
    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) => Task.CompletedTask;
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) => Task.CompletedTask;
}
