using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using FellsideDigital.Web.Services.Live;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FellsideDigital.Web.Components.Pages.Live;

public partial class Join : ComponentBase
{
    [Inject] private IQrLeadService QrLeadService { get; set; } = default!;
    [Inject] private IEmailService EmailService { get; set; } = default!;
    [Inject] private LiveShowcaseState Live { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<Join> Logger { get; set; } = default!;

    private string _name = "";
    private string _email = "";
    private string _error = "";
    private string? _userAgent;
    private bool _saving;
    private bool _submitted;
    private string _successMessage = "Look up at the big screen — that's you.";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _userAgent is not null) return;
        try
        {
            _userAgent = await JS.InvokeAsync<string>("fellsideDevice.userAgent");
        }
        catch (Exception ex)
        {
            // Non-fatal: device stays "Other" in the metrics if this fails.
            Logger.LogWarning(ex, "Could not read user-agent for live device metrics");
        }
    }

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

        var device = DeviceDetector.Classify(_userAgent);
        var domain = email.Contains('@') ? email[(email.LastIndexOf('@') + 1)..].ToLowerInvariant() : null;
        Live.Publish(new LiveParticipant(name, company, DateTimeOffset.UtcNow, device, domain));

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
