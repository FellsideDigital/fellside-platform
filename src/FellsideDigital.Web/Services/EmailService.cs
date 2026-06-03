using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using EmailSettings = FellsideDigital.Web.Models.EmailSettings;

namespace FellsideDigital.Web.Services;

public class EmailService : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly Lazy<byte[]?> _logoBytes;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger, IWebHostEnvironment env)
    {
        _settings = settings.Value;
        _logger = logger;
        _env = env;
        _logoBytes = new Lazy<byte[]?>(LoadLogo);
    }

    // ── IEmailSender<ApplicationUser> (Identity's built-in flows) ──────────────

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Fellside Digital account", EmailTemplates.Confirmation(confirmationLink));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Fellside Digital password", EmailTemplates.PasswordReset(resetLink));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Your password reset code", EmailTemplates.PasswordResetCode(resetCode));

    // ── Custom emails ──────────────────────────────────────────────────────────

    public Task SendInvitationAsync(ClientInvitation invitation, string registrationUrl) =>
        SendAsync(
            invitation.Email,
            "You've been invited to your Fellside Digital client portal",
            EmailTemplates.Invitation(invitation, registrationUrl));

    public Task SendClientRegisteredNotificationAsync(ApplicationUser user) =>
        SendAsync(
            _settings.AdminEmail,
            $"New client registered: {user.FirstName} {user.LastName}",
            EmailTemplates.AdminNotification(user));

    public Task SendWelcomeEmailAsync(ApplicationUser user) =>
        SendAsync(
            user.Email!,
            "Welcome to your Fellside Digital client portal",
            EmailTemplates.Welcome(user));

    public Task SendContactEnquiryAsync(ContactEnquiry enquiry) =>
        SendAsync(
            _settings.AdminEmail,
            $"New enquiry from {enquiry.Name} — {enquiry.ServiceType}",
            EmailTemplates.ContactEnquiry(enquiry));

    public Task SendQrLeadDiscountAsync(QrLead lead) =>
        SendAsync(
            lead.Email,
            "Your exclusive Fellside Digital offer — LAUNCH26",
            EmailTemplates.QrLeadDiscount(lead));

    // ── Portal activity notifications (client, admin BCC'd as a receipt) ────────

    public Task SendDocumentAddedAsync(ApplicationUser client, ClientProject project, string documentTitle, string portalUrl) =>
        SendAsync(
            client.Email!,
            $"New document on your {project.Name} project",
            EmailTemplates.DocumentAdded(client, project, documentTitle, portalUrl),
            bccAdmin: true);

    public Task SendInvoiceAddedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl) =>
        SendAsync(
            client.Email!,
            $"New invoice for your {project.Name} project",
            EmailTemplates.InvoiceAdded(client, project, invoice, portalUrl),
            bccAdmin: true);

    public Task SendInvoiceStatusChangedAsync(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl) =>
        SendAsync(
            client.Email!,
            $"Invoice update for your {project.Name} project",
            EmailTemplates.InvoiceStatusChanged(client, project, invoice, portalUrl),
            bccAdmin: true);

    // ── Core send ──────────────────────────────────────────────────────────────

    private async Task SendAsync(string to, string subject, string htmlBody, bool bccAdmin = false)
    {
        if (string.IsNullOrWhiteSpace(_settings.TenantId) ||
            string.IsNullOrWhiteSpace(_settings.ClientId) ||
            string.IsNullOrWhiteSpace(_settings.ClientSecret) ||
            string.IsNullOrWhiteSpace(_settings.FromAddress))
        {
            throw new InvalidOperationException(
                "Email is not configured. Ensure Email:TenantId, Email:ClientId, Email:ClientSecret, " +
                "and Email:FromAddress are set in environment variables (e.g. Email__FromAddress on Railway).");
        }

        try
        {
            var credential = new ClientSecretCredential(
                _settings.TenantId,
                _settings.ClientId,
                _settings.ClientSecret);

            var graphClient = new GraphServiceClient(credential);

            var message = new Message
            {
                Subject = subject,
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = htmlBody
                },
                ToRecipients =
                [
                    new Recipient { EmailAddress = new EmailAddress { Address = to } }
                ],
                Attachments = BuildLogoAttachment()
            };

            if (bccAdmin && !string.IsNullOrWhiteSpace(_settings.AdminEmail) &&
                !string.Equals(_settings.AdminEmail, to, StringComparison.OrdinalIgnoreCase))
            {
                message.BccRecipients =
                [
                    new Recipient { EmailAddress = new EmailAddress { Address = _settings.AdminEmail } }
                ];
            }

            await graphClient.Users[_settings.FromAddress]
                .SendMail
                .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });

            _logger.LogInformation("Email sent to {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            throw;
        }
    }

    /// <summary>The brand logo as an inline attachment, referenced by the templates as cid:fellside-logo.</summary>
    private List<Attachment>? BuildLogoAttachment()
    {
        if (_logoBytes.Value is not { } bytes) return null;

        return
        [
            new FileAttachment
            {
                OdataType   = "#microsoft.graph.fileAttachment",
                Name        = "logo.png",
                ContentType = "image/png",
                ContentBytes = bytes,
                ContentId   = EmailTheme.LogoContentId,
                IsInline    = true,
            }
        ];
    }

    private byte[]? LoadLogo()
    {
        try
        {
            var path = Path.Combine(_env.WebRootPath, "web-app-manifest-512x512.png");
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load email logo; emails will render without it.");
            return null;
        }
    }
}
