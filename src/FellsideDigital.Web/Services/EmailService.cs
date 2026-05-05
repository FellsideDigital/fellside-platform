using FellsideDigital.Web.Data;
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

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    // ── IEmailSender<ApplicationUser> (Identity's built-in flows) ──────────────

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your Fellside Digital account", ConfirmationTemplate(confirmationLink));

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Fellside Digital password", PasswordResetTemplate(resetLink));

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Your password reset code", $"<p>Your reset code is: <strong>{resetCode}</strong></p>");

    // ── Custom emails ──────────────────────────────────────────────────────────

    public Task SendInvitationAsync(ClientInvitation invitation, string registrationUrl) =>
        SendAsync(
            invitation.Email,
            "You've been invited to your Fellside Digital client portal",
            InvitationTemplate(invitation, registrationUrl));

    public Task SendClientRegisteredNotificationAsync(ApplicationUser user) =>
        SendAsync(
            _settings.AdminEmail,
            $"New client registered: {user.FirstName} {user.LastName}",
            AdminNotificationTemplate(user));

    public Task SendWelcomeEmailAsync(ApplicationUser user) =>
        SendAsync(
            user.Email!,
            "Welcome to your Fellside Digital client portal",
            WelcomeTemplate(user));

    public Task SendContactEnquiryAsync(ContactEnquiry enquiry) =>
        SendAsync(
            _settings.AdminEmail,
            $"New enquiry from {enquiry.Name} — {enquiry.ServiceType}",
            ContactEnquiryTemplate(enquiry));

    // ── Core send ──────────────────────────────────────────────────────────────

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
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
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = to
                        }
                    }
                ]
            };

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

// ── Templates ──────────────────────────────────────────────────────────────

private static string InvitationTemplate(ClientInvitation inv, string url) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:24px;line-height:1.25;color:#0f172a;">You're invited to your client portal</h2>
            <p style="margin:0 0 18px;color:#475569;font-size:15px;line-height:1.6;">
                Hi {inv.FirstName}, your Fellside Digital workspace is ready.
                Use the button below to set your password and activate your account.
            </p>

            <table role="presentation" style="width:100%;border-collapse:separate;border-spacing:0;margin:0 0 20px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;overflow:hidden;">
                <tr><td style="padding:12px 14px;color:#64748b;font-size:13px;width:140px;border-bottom:1px solid #e2e8f0;">Company</td><td style="padding:12px 14px;font-size:14px;color:#0f172a;border-bottom:1px solid #e2e8f0;">{inv.CompanyName}</td></tr>
                <tr><td style="padding:12px 14px;color:#64748b;font-size:13px;width:140px;border-bottom:1px solid #e2e8f0;">Service</td><td style="padding:12px 14px;font-size:14px;color:#0f172a;border-bottom:1px solid #e2e8f0;">{inv.ServiceType}</td></tr>
                <tr><td style="padding:12px 14px;color:#64748b;font-size:13px;width:140px;vertical-align:top;">Project</td><td style="padding:12px 14px;font-size:14px;color:#0f172a;line-height:1.55;">{inv.ProjectDescription}</td></tr>
            </table>

            <p style="margin:0 0 22px;color:#64748b;font-size:13px;line-height:1.6;">
                This invitation expires on <strong style="color:#0f172a;">{inv.ExpiresAt:dddd, d MMMM yyyy}</strong>.
            </p>

            <div style="margin:0 0 20px;">
                <a href="{url}" style="display:inline-block;background:#fb923c;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:700;font-size:14px;letter-spacing:.2px;">
                    Set up your account →
                </a>
            </div>

            <p style="margin:0;color:#94a3b8;font-size:12px;line-height:1.6;word-break:break-all;">
                If the button doesn't work, copy and paste this link into your browser:<br/>
                <a href="{url}" style="color:#6366f1;text-decoration:underline;">{url}</a>
            </p>
        """)}
        """;

    private static string AdminNotificationTemplate(ApplicationUser user) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:22px;color:#111827;">New client registered</h2>
            <p style="margin:0 0 16px;color:#6b7280;font-size:15px;">A client has completed their account setup.</p>

            <table style="width:100%;border-collapse:collapse;margin-bottom:24px;">
                <tr><td style="padding:6px 0;color:#6b7280;font-size:14px;width:140px;">Name</td><td style="padding:6px 0;font-size:14px;color:#111827;">{user.FirstName} {user.LastName}</td></tr>
                <tr><td style="padding:6px 0;color:#6b7280;font-size:14px;">Company</td><td style="padding:6px 0;font-size:14px;color:#111827;">{user.CompanyName}</td></tr>
                <tr><td style="padding:6px 0;color:#6b7280;font-size:14px;">Email</td><td style="padding:6px 0;font-size:14px;color:#111827;">{user.Email}</td></tr>
                <tr><td style="padding:6px 0;color:#6b7280;font-size:14px;">Service</td><td style="padding:6px 0;font-size:14px;color:#111827;">{user.ServiceType}</td></tr>
                <tr><td style="padding:6px 0;color:#6b7280;font-size:14px;vertical-align:top;">Project</td><td style="padding:6px 0;font-size:14px;color:#111827;">{user.ProjectDescription}</td></tr>
            </table>
        """)}
        """;

    private static string WelcomeTemplate(ApplicationUser user) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:22px;color:#111827;">Welcome, {user.FirstName}!</h2>
            <p style="margin:0 0 16px;color:#6b7280;font-size:15px;">
                Your Fellside Digital client portal account is live. You can now log in to track progress, review updates, and communicate with the team.
            </p>
            <p style="margin:0 0 8px;color:#6b7280;font-size:14px;">Your account email: <strong style="color:#111827;">{user.Email}</strong></p>
        """)}
        """;

    private static string ConfirmationTemplate(string url) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:22px;color:#111827;">Confirm your email</h2>
            <p style="margin:0 0 24px;color:#6b7280;font-size:15px;">Click the button below to confirm your email address.</p>
            <a href="{url}" style="display:inline-block;background:#f97316;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:600;font-size:15px;">
                Confirm email →
            </a>
        """)}
        """;

    private static string PasswordResetTemplate(string url) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:22px;color:#111827;">Reset your password</h2>
            <p style="margin:0 0 24px;color:#6b7280;font-size:15px;">Click the button below to choose a new password. This link expires in 1 hour.</p>
            <a href="{url}" style="display:inline-block;background:#f97316;color:#fff;text-decoration:none;padding:12px 28px;border-radius:6px;font-weight:600;font-size:15px;">
                Reset password →
            </a>
        """)}
        """;

    private static string ContactEnquiryTemplate(ContactEnquiry e) => $"""
        {BaseLayout($"""
            <h2 style="margin:0 0 8px;font-size:22px;color:#111827;">New contact enquiry</h2>
            <p style="margin:0 0 16px;color:#6b7280;font-size:15px;">Someone submitted the contact form on your website.</p>

            <table style="width:100%;border-collapse:collapse;margin-bottom:24px;">
                <tr style="border-bottom:1px solid #f1f5f9;"><td style="padding:8px 0;color:#6b7280;font-size:14px;width:140px;">Name</td><td style="padding:8px 0;font-size:14px;color:#111827;">{e.Name}</td></tr>
                <tr style="border-bottom:1px solid #f1f5f9;"><td style="padding:8px 0;color:#6b7280;font-size:14px;">Email</td><td style="padding:8px 0;font-size:14px;"><a href="mailto:{e.Email}" style="color:#6366f1;text-decoration:underline;">{e.Email}</a></td></tr>
                {(e.Phone is not null ? $"<tr style=\"border-bottom:1px solid #f1f5f9;\"><td style=\"padding:8px 0;color:#6b7280;font-size:14px;\">Phone</td><td style=\"padding:8px 0;font-size:14px;color:#111827;\">{e.Phone}</td></tr>" : "")}
                {(e.Company is not null ? $"<tr style=\"border-bottom:1px solid #f1f5f9;\"><td style=\"padding:8px 0;color:#6b7280;font-size:14px;\">Company</td><td style=\"padding:8px 0;font-size:14px;color:#111827;\">{e.Company}</td></tr>" : "")}
                <tr style="border-bottom:1px solid #f1f5f9;"><td style="padding:8px 0;color:#6b7280;font-size:14px;">Service</td><td style="padding:8px 0;font-size:14px;color:#111827;">{e.ServiceType}</td></tr>
                {(e.Budget is not null ? $"<tr style=\"border-bottom:1px solid #f1f5f9;\"><td style=\"padding:8px 0;color:#6b7280;font-size:14px;\">Budget</td><td style=\"padding:8px 0;font-size:14px;color:#111827;\">{e.Budget}</td></tr>" : "")}
                {(e.HowHeard is not null ? $"<tr style=\"border-bottom:1px solid #f1f5f9;\"><td style=\"padding:8px 0;color:#6b7280;font-size:14px;\">Via</td><td style=\"padding:8px 0;font-size:14px;color:#111827;\">{e.HowHeard}</td></tr>" : "")}
            </table>

            <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:16px 20px;margin-bottom:24px;">
                <p style="margin:0 0 6px;font-size:13px;color:#6b7280;font-weight:600;text-transform:uppercase;letter-spacing:.5px;">Message</p>
                <p style="margin:0;font-size:14px;color:#0f172a;line-height:1.65;white-space:pre-wrap;">{e.Message}</p>
            </div>

            <a href="mailto:{e.Email}" style="display:inline-block;background:#6366f1;color:#ffffff;text-decoration:none;padding:12px 24px;border-radius:8px;font-weight:700;font-size:14px;letter-spacing:.2px;">
                Reply to {e.Name} →
            </a>
        """)}
        """;

    private static string BaseLayout(string content) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f1f5f9;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
            <table width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:32px 14px;">
                <tr><td align="center">
                    <table width="100%" cellpadding="0" cellspacing="0" style="max-width:620px;background:#ffffff;border-radius:14px;overflow:hidden;border:1px solid #e2e8f0;box-shadow:0 1px 3px rgba(15,23,42,.07);">
                        <tr>
                            <td style="background:#0f172a;padding:20px 24px;">
                                <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                                    <tr>
                                        <td style="vertical-align:middle;">
                                            <span style="display:inline-flex;vertical-align:middle;align-items:center;gap:10px;">
                                                <svg xmlns="http://www.w3.org/2000/svg" width="28" height="28" viewBox="0 0 24 24" fill="none" aria-hidden="true" style="display:inline-block;vertical-align:middle;">
                                                    <path d="M3.75 13.5l10.5-11.25L12 10.5h8.25L9.75 21.75 12 13.5H3.75z" fill="#fb923c"/>
                                                </svg>
                                                <span style="color:#ffffff;font-weight:700;font-size:18px;letter-spacing:-0.2px;">Fellside Digital</span>
                                            </span>
                                        </td>
                                    </tr>
                                </table>
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:30px 24px 26px;">
                                {content}
                            </td>
                        </tr>
                        <tr>
                            <td style="padding:16px 24px;border-top:1px solid #f1f5f9;color:#94a3b8;font-size:12px;line-height:1.5;">
                                Fellside Digital · Cumbria, UK<br/>
                                This is an automated message, please do not reply.
                            </td>
                        </tr>
                    </table>
                </td></tr>
            </table>
        </body>
        </html>
        """;
}
