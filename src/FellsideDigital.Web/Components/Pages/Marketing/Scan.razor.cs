using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Marketing;

public partial class Scan : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;

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

        Db.QrLeads.Add(lead);
        await Db.SaveChangesAsync();

        _submitted = true;
        _saving    = false;
    }
}
