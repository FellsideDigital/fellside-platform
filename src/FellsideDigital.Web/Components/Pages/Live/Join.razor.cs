using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Live;

public partial class Join : ComponentBase
{
    [Inject] private IQrLeadService QrLeadService { get; set; } = default!;
    [Inject] private IEmailService EmailService { get; set; } = default!;
    [Inject] private LiveShowcaseState Live { get; set; } = default!;
    [Inject] private ILogger<Join> Logger { get; set; } = default!;

    private string _name = "";
    private string _email = "";
    private string _error = "";
    private bool _saving;
    private bool _submitted;
    private string _successMessage = "Look up at the big screen — that's you.";

    private async Task SubmitAsync()
    {
        _error = "";
        var name = _name.Trim();
        var email = _email.Trim();

        if (name.Length == 0 || !EmailValidator.IsValid(email))
        {
            _error = "Please enter your name and a valid email address.";
            return;
        }

        _saving = true;
        var company = CompanyResolver.Resolve(email);

        var lead = new QrLead
        {
            Source = "live",
            Name = name,
            Email = email,
            Company = company,
            Interest = "Automation",
        };

        try
        {
            await QrLeadService.CreateLeadAsync(lead);
        }
        catch (Exception ex)
        {
            _error = ErrorHandling.LogAndDescribe(Logger, ex, "triggering the automation");
            _saving = false;
            return;
        }

        Live.Publish(new LiveParticipant(name, company, DateTimeOffset.UtcNow));

        var emailed = true;
        try
        {
            await EmailService.SendLiveAutomationWelcomeAsync(lead);
        }
        catch (Exception ex)
        {
            emailed = false;
            Logger.LogError(ex, "Live welcome email failed for {Email}", email);
        }

        _successMessage = emailed
            ? "Look up at the big screen — and check your inbox in a moment."
            : "Look up at the big screen — that's you.";
        _submitted = true;
        _saving = false;
    }
}
