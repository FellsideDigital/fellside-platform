using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;

namespace FellsideDigital.Web.Services.Email;

/// <summary>
/// Pure HTML body renderers for every transactional email. No I/O — testable in
/// isolation. All branding flows through <see cref="EmailTheme"/>; do not hardcode
/// colours or button markup here.
/// </summary>
internal static class EmailTemplates
{
    private static string Greeting(string? firstName) =>
        string.IsNullOrWhiteSpace(firstName) ? "Hi there," : $"Hi {firstName},";

    private static string Money(Invoice invoice) => $"{invoice.Currency} {invoice.Amount:N2}";

    private static string H2(string text) =>
        $"""<h2 style="margin:0 0 8px;font-size:23px;line-height:1.25;color:{EmailTheme.Heading};">{text}</h2>""";

    private static string P(string text) =>
        $"""<p style="margin:0 0 18px;color:{EmailTheme.Body};font-size:15px;line-height:1.6;">{text}</p>""";

    // ── Identity flows ──────────────────────────────────────────────────────

    public static string Confirmation(string url) => EmailTheme.Layout($"""
        {H2("Confirm your email")}
        {P("Click the button below to confirm your email address.")}
        {EmailTheme.Button(url, "Confirm email →")}
        """);

    public static string PasswordReset(string url) => EmailTheme.Layout($"""
        {H2("Reset your password")}
        {P("Click the button below to choose a new password. This link expires in 1 hour.")}
        {EmailTheme.Button(url, "Reset password →")}
        """);

    public static string PasswordResetCode(string code) => EmailTheme.Layout($"""
        {H2("Your password reset code")}
        {P("Use the code below to reset your password.")}
        <p style="margin:0;font-size:30px;font-weight:800;letter-spacing:6px;color:{EmailTheme.AccentDeep};font-family:monospace;">{code}</p>
        """);

    // ── Onboarding ──────────────────────────────────────────────────────────

    public static string Invitation(ClientInvitation inv, string url) => EmailTheme.Layout($"""
        {H2("You're invited to your client portal")}
        {P($"Hi {inv.FirstName}, your Fellside Digital workspace is ready. Use the button below to set your password and activate your account.")}
        {EmailTheme.InfoTable([
            ("Company", inv.CompanyName),
            ("Service", inv.ServiceType),
            ("Project", inv.ProjectDescription),
        ])}
        <p style="margin:0 0 22px;color:{EmailTheme.Muted};font-size:13px;line-height:1.6;">
            This invitation expires on <strong style="color:{EmailTheme.Heading};">{inv.ExpiresAt:dddd, d MMMM yyyy}</strong>.
        </p>
        <div style="margin:0 0 20px;">{EmailTheme.Button(url, "Set up your account →")}</div>
        <p style="margin:0;color:{EmailTheme.Faint};font-size:12px;line-height:1.6;word-break:break-all;">
            If the button doesn't work, copy and paste this link into your browser:<br/>
            <a href="{url}" style="color:{EmailTheme.AccentDeep};text-decoration:underline;">{url}</a>
        </p>
        """);

    public static string Welcome(ApplicationUser user) => EmailTheme.Layout($"""
        {H2($"Welcome, {user.FirstName}!")}
        {P("Your Fellside Digital client portal account is live. You can now log in to track progress, review updates, and communicate with the team.")}
        <p style="margin:0 0 8px;color:{EmailTheme.Body};font-size:14px;">Your account email: <strong style="color:{EmailTheme.Heading};">{user.Email}</strong></p>
        """);

    // ── Admin-facing ────────────────────────────────────────────────────────

    public static string AdminNotification(ApplicationUser user) => EmailTheme.Layout($"""
        {H2("New client registered")}
        {P("A client has completed their account setup.")}
        {EmailTheme.InfoTable([
            ("Name", $"{user.FirstName} {user.LastName}"),
            ("Company", user.CompanyName ?? "—"),
            ("Email", user.Email ?? "—"),
            ("Service", user.ServiceType ?? "—"),
            ("Project", user.ProjectDescription ?? "—"),
        ])}
        """);

    public static string ContactEnquiry(ContactEnquiry e)
    {
        var rows = new List<(string, string)> { ("Name", e.Name), ("Email", e.Email) };
        if (e.Phone is not null)    rows.Add(("Phone", e.Phone));
        if (e.Company is not null)  rows.Add(("Company", e.Company));
        rows.Add(("Service", e.ServiceType));
        if (e.Budget is not null)   rows.Add(("Budget", e.Budget));
        if (e.HowHeard is not null) rows.Add(("Via", e.HowHeard));

        return EmailTheme.Layout($"""
            {H2("New contact enquiry")}
            {P("Someone submitted the contact form on your website.")}
            {EmailTheme.InfoTable(rows)}
            <div style="background:{EmailTheme.SurfaceMute};border:1px solid {EmailTheme.Border};border-radius:10px;padding:16px 20px;margin-bottom:24px;">
                <p style="margin:0 0 6px;font-size:13px;color:{EmailTheme.Muted};font-weight:600;text-transform:uppercase;letter-spacing:.5px;">Message</p>
                <p style="margin:0;font-size:14px;color:{EmailTheme.Heading};line-height:1.65;white-space:pre-wrap;">{e.Message}</p>
            </div>
            {EmailTheme.Button($"mailto:{e.Email}", $"Reply to {e.Name} →")}
            """);
    }

    // ── Marketing ───────────────────────────────────────────────────────────

    public static string QrLeadDiscount(QrLead lead) => EmailTheme.Layout($"""
        {H2($"Nice to meet you, {lead.Name}!")}
        {P("Thanks for scanning — here's your exclusive discount code, saved in your inbox so you never lose it.")}

        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 28px;background:{EmailTheme.AccentSoft};border:2px solid {EmailTheme.AccentLight};border-radius:12px;overflow:hidden;">
            <tr><td style="padding:24px;text-align:center;">
                <p style="margin:0 0 6px;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:1px;color:{EmailTheme.AccentDeep};">Your discount code</p>
                <p style="margin:0 0 4px;font-size:36px;font-weight:800;letter-spacing:4px;color:{EmailTheme.AccentButton};font-family:monospace;">LAUNCH26</p>
                <p style="margin:0;font-size:12px;color:{EmailTheme.AccentDeep};">Valid for 60 days · 15% off your first project</p>
            </td></tr>
        </table>

        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 28px;background:{EmailTheme.SurfaceMute};border:1px solid {EmailTheme.Border};border-radius:10px;overflow:hidden;">
            <tr><td style="padding:12px 16px;border-bottom:1px solid {EmailTheme.Border};">
                <span style="font-size:14px;color:{EmailTheme.Heading};">✓ &nbsp;<strong>15% off</strong> your first project — applied at quote time</span>
            </td></tr>
            <tr><td style="padding:12px 16px;">
                <span style="font-size:14px;color:{EmailTheme.Heading};">✓ &nbsp;<strong>Free 30-minute discovery call</strong> — no obligation, no sales pitch</span>
            </td></tr>
        </table>

        <p style="margin:0 0 10px;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.8px;color:{EmailTheme.Faint};">What happens next</p>
        <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 28px;">
            {QrStep(1, "I'll be in touch within 24 hours to say hello.")}
            {QrStep(2, "We'll book your free discovery call at a time that suits you.")}
            {QrStep(3, "When we put together your quote, LAUNCH26 gets applied automatically.")}
        </table>

        {EmailTheme.Button("mailto:hello@fellsidedigital.co.uk", "Get in touch →")}
        """);

    private static string QrStep(int n, string text) => $"""
        <tr><td style="padding:6px 0;font-size:14px;color:{EmailTheme.Body};line-height:1.6;">
            <span style="display:inline-block;width:22px;height:22px;background:{EmailTheme.AccentSoft};border-radius:50%;text-align:center;line-height:22px;font-size:12px;font-weight:700;color:{EmailTheme.AccentButton};margin-right:10px;vertical-align:middle;">{n}</span>
            {text}
        </td></tr>
        """;

    // ── Portal activity notifications ───────────────────────────────────────

    public static string DocumentAdded(ApplicationUser client, ClientProject project, string documentTitle, string portalUrl) =>
        EmailTheme.Layout($"""
            {H2("A new document is ready")}
            {P($"{Greeting(client.FirstName)} a new document has been shared on your <strong>{project.Name}</strong> project. You can view and download it from your portal.")}
            {EmailTheme.InfoTable([
                ("Document", documentTitle),
                ("Project", project.Name),
            ])}
            <div style="margin:0 0 4px;">{EmailTheme.Button(portalUrl, "View in your portal →")}</div>
            """);

    public static string InvoiceAdded(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl) =>
        EmailTheme.Layout($"""
            {H2("You have a new invoice")}
            {P($"{Greeting(client.FirstName)} a new invoice has been issued for your <strong>{project.Name}</strong> project.")}
            {EmailTheme.InfoTable(InvoiceRows(project, invoice))}
            <div style="margin:0 0 4px;">{EmailTheme.Button(portalUrl, "View invoice →")}</div>
            """);

    public static string InvoiceStatusChanged(ApplicationUser client, ClientProject project, Invoice invoice, string portalUrl)
    {
        var (heading, intro) = invoice.Status switch
        {
            InvoiceStatus.Overdue => ("An invoice is now overdue",
                $"this is a reminder that the invoice below for your <strong>{project.Name}</strong> project is now overdue. Please review it at your earliest convenience."),
            _ => ("Your invoice has been sent",
                $"an invoice has been sent for your <strong>{project.Name}</strong> project. The details are below."),
        };

        return EmailTheme.Layout($"""
            {H2(heading)}
            {P($"{Greeting(client.FirstName)} {intro}")}
            {EmailTheme.InfoTable(InvoiceRows(project, invoice))}
            <div style="margin:0 0 4px;">{EmailTheme.Button(portalUrl, "View invoice →")}</div>
            """);
    }

    private static List<(string, string)> InvoiceRows(ClientProject project, Invoice invoice)
    {
        var rows = new List<(string, string)>
        {
            ("Invoice", invoice.Title),
            ("Amount", Money(invoice)),
        };
        if (invoice.DueAt is { } due) rows.Add(("Due", due.ToString("d MMMM yyyy")));
        rows.Add(("Project", project.Name));
        return rows;
    }
}
