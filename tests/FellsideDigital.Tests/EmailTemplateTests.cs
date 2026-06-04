using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services.Email;

namespace FellsideDigital.Tests;

/// <summary>
/// Pure-logic tests over the centralised email rendering. No DB/fixture needed.
/// Guards the blue rebrand, the inline logo, and the notification CTAs.
/// </summary>
public class EmailTemplateTests
{
    // Legacy orange/indigo accents that must never reappear.
    private static readonly string[] BannedColours = ["#fb923c", "#f97316", "#fff7ed", "#6366f1", "#9a3412", "#c2410c"];

    private static ApplicationUser Client() => new()
    {
        FirstName = "Ada",
        LastName = "Lovelace",
        Email = "ada@example.com",
    };

    private static ClientProject Project() => new() { Id = Guid.NewGuid(), Name = "Acme Rebuild" };

    private static Invoice SampleInvoice() => new()
    {
        Title = "Milestone 1",
        Amount = 1200.50m,
        Currency = "GBP",
        DueAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        Status = InvoiceStatus.Sent,
    };

    private static void AssertNoBannedColours(string html)
    {
        foreach (var colour in BannedColours)
            Assert.DoesNotContain(colour, html);
    }

    [Fact]
    public void Layout_uses_blue_palette_and_inline_logo()
    {
        var html = EmailTemplates.Confirmation("https://fellsidedigital.co.uk/confirm");

        AssertNoBannedColours(html);
        Assert.Contains(EmailTheme.AccentButton, html);          // blue-600 button
        Assert.Contains($"cid:{EmailTheme.LogoContentId}", html); // inline logo reference
        Assert.Contains("Fellside Digital", html);
    }

    [Fact]
    public void Button_fill_uses_accent_for_contrast()
    {
        var button = EmailTheme.Button("https://x", "Go");
        Assert.Contains(EmailTheme.AccentButton, button);
    }

    [Theory]
    [MemberData(nameof(AllTemplates))]
    public void Every_template_is_blue_and_branded(string html)
    {
        AssertNoBannedColours(html);
        Assert.Contains($"cid:{EmailTheme.LogoContentId}", html);
        Assert.Contains("<!DOCTYPE html>", html);
    }

    public static IEnumerable<object[]> AllTemplates()
    {
        var url = "https://fellsidedigital.co.uk/Portal/Projects/abc";
        yield return [EmailTemplates.Confirmation(url)];
        yield return [EmailTemplates.PasswordReset(url)];
        yield return [EmailTemplates.PasswordResetCode("482913")];
        yield return [EmailTemplates.Welcome(Client())];
        yield return [EmailTemplates.AdminNotification(Client())];
        yield return [EmailTemplates.Invitation(new ClientInvitation
        {
            FirstName = "Ada", CompanyName = "Acme", ServiceType = "Website",
            ProjectDescription = "Rebuild", Email = "ada@example.com",
            ExpiresAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        }, url)];
        yield return [EmailTemplates.ContactEnquiry(new ContactEnquiry
        {
            Name = "Ada", Email = "ada@example.com", ServiceType = "Website", Message = "Hi",
        })];
        yield return [EmailTemplates.QrLeadDiscount(new QrLead { Name = "Ada", Email = "ada@example.com" })];
        yield return [EmailTemplates.QrLeadNotification(new QrLead
        {
            Name = "Ada", Email = "ada@example.com", Source = "shirt", Interest = "Both",
        })];
        yield return [EmailTemplates.DocumentAdded(Client(), Project(), "Contract.pdf", url)];
        yield return [EmailTemplates.InvoiceAdded(Client(), Project(), SampleInvoice(), url)];
        yield return [EmailTemplates.InvoiceStatusChanged(Client(), Project(), SampleInvoice(), url)];
    }

    [Fact]
    public void QrLeadNotification_includes_contact_details_interest_and_reply_cta()
    {
        var lead = new QrLead
        {
            Name = "Ada", Email = "ada@example.com", Phone = "07700 900000",
            Company = "Acme", Source = "shirt", Interest = "Automation",
            Budget = "£1k–£3k", Timeline = "ASAP", Message = "Keen to chat",
        };

        var html = EmailTemplates.QrLeadNotification(lead);

        Assert.Contains("Ada", html);
        Assert.Contains("ada@example.com", html);
        Assert.Contains("07700 900000", html);
        Assert.Contains("Acme", html);
        Assert.Contains("Automation", html);
        Assert.Contains("£1k–£3k", html);
        Assert.Contains("ASAP", html);
        Assert.Contains("Keen to chat", html);
        Assert.Contains("mailto:ada@example.com", html); // reply CTA
    }

    [Fact]
    public void DocumentAdded_includes_title_project_and_cta()
    {
        var project = Project();
        var url = $"https://fellsidedigital.co.uk/Portal/Projects/{project.Id}";

        var html = EmailTemplates.DocumentAdded(Client(), project, "Brand Guidelines.pdf", url);

        Assert.Contains("Brand Guidelines.pdf", html);
        Assert.Contains(project.Name, html);
        Assert.Contains(url, html);
        Assert.Contains("Ada", html); // greeting uses first name
    }

    [Fact]
    public void InvoiceAdded_shows_amount_currency_and_due_date()
    {
        var html = EmailTemplates.InvoiceAdded(Client(), Project(), SampleInvoice(), "https://x");

        Assert.Contains("GBP 1,200.50", html);
        Assert.Contains("Milestone 1", html);
        Assert.Contains("1 July 2026", html);
    }

    [Fact]
    public void InvoiceStatusChanged_overdue_reads_as_a_reminder()
    {
        var invoice = SampleInvoice();
        invoice.Status = InvoiceStatus.Overdue;

        var html = EmailTemplates.InvoiceStatusChanged(Client(), Project(), invoice, "https://x");

        Assert.Contains("overdue", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Greeting_falls_back_when_no_first_name()
    {
        var client = Client();
        client.FirstName = null;

        var html = EmailTemplates.DocumentAdded(client, Project(), "x", "https://x");

        Assert.Contains("Hi there", html);
    }
}
