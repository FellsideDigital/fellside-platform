using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Marketing;

public partial class Scan : ComponentBase
{
    [Inject] private IQrLeadService QrLeadService { get; set; } = default!;
    [Inject] private EmailService   EmailService  { get; set; } = default!;
    [Inject] private ILogger<Scan>  Logger        { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "from")] public string? From { get; set; }
    [SupplyParameterFromQuery(Name = "ref")]  public string? Ref  { get; set; }

    private string SourceLabel => From switch
    {
        "shirt" => "You scanned my t-shirt",
        "card"  => "You scanned my business card",
        _       => "You found Fellside Digital",
    };

    private readonly string[] _interestOptions = ["Website design", "Automation", "Both", "Not sure yet"];
    private readonly string[] _budgetOptions    = ["Under £1k", "£1k–£3k", "£3k–£10k", "£10k+"];
    private readonly string[] _timelineOptions  = ["ASAP", "1–3 months", "3–6 months", "Just exploring"];

    private string _name     = "";
    private string _email    = "";
    private string _phone    = "";
    private string _company  = "";
    private string _interest = "";
    private string _budget   = "";
    private string _timeline = "";
    private string _message  = "";
    private string _error    = "";
    private bool   _saving;
    private bool   _submitted;

    private const string InputClass =
        "w-full rounded-xl px-4 py-2.5 text-sm " +
        "bg-slate-50 dark:bg-white/5 " +
        "border border-slate-200 dark:border-white/10 " +
        "text-slate-900 dark:text-white " +
        "placeholder:text-slate-400 dark:placeholder:text-neutral-600 " +
        "focus:outline-none focus:ring-2 focus:ring-accent/50 transition";

    private static string ToggleClass(bool active) => active
        ? "rounded-xl border px-4 py-2.5 text-sm text-left transition-colors " +
          "bg-accent/20 border-accent text-accent"
        : "rounded-xl border px-4 py-2.5 text-sm text-left transition-colors " +
          "bg-slate-100 dark:bg-white/5 border-slate-200 dark:border-white/10 " +
          "text-slate-600 dark:text-neutral-400 hover:border-slate-300 dark:hover:border-white/20";

    private void ToggleBudget(string value)   => _budget   = _budget   == value ? "" : value;
    private void ToggleTimeline(string value) => _timeline = _timeline == value ? "" : value;

    private async Task SubmitAsync()
    {
        _error = "";

        if (string.IsNullOrWhiteSpace(_name))     { _error = "Please enter your name.";                    return; }
        if (string.IsNullOrWhiteSpace(_email))    { _error = "Please enter your email.";                   return; }
        if (string.IsNullOrWhiteSpace(_interest)) { _error = "Please select what you need help with.";     return; }

        _saving = true;

        var lead = new QrLead
        {
            Source   = From ?? "unknown",
            Name     = _name.Trim(),
            Email    = _email.Trim(),
            Phone    = string.IsNullOrWhiteSpace(_phone)    ? null : _phone.Trim(),
            Company  = string.IsNullOrWhiteSpace(_company)  ? null : _company.Trim(),
            Interest = _interest,
            Budget   = string.IsNullOrWhiteSpace(_budget)   ? null : _budget,
            Timeline = string.IsNullOrWhiteSpace(_timeline) ? null : _timeline,
            Message  = string.IsNullOrWhiteSpace(_message)  ? null : _message.Trim(),
            QrScanId = Guid.TryParse(Ref, out var scanId)   ? scanId : null,
        };

        try
        {
            await QrLeadService.CreateLeadAsync(lead);
        }
        catch (Exception ex)
        {
            _error  = ErrorHandling.LogAndDescribe(Logger, ex, "submitting your details");
            _saving = false;
            return;
        }

        try { await EmailService.SendQrLeadDiscountAsync(lead); }
        catch (Exception ex) { Logger.LogError(ex, "Failed to send QR discount email to {Email}", lead.Email); }

        _submitted = true;
        _saving    = false;
    }
}
