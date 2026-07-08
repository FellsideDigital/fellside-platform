using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Marketing;

public partial class Quote : ComponentBase
{
    [Inject] private IQuoteEstimatorService Estimator { get; set; } = default!;
    [Inject] private IEnquiryService EnquiryService { get; set; } = default!;
    [Inject] private IEmailService EmailService { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Quote> Logger { get; set; } = default!;

    // ── Option lists (label + display price) ──────────────────────────────
    private static readonly (WebsiteType Type, string Label, string Price)[] WebsiteTypeOptions =
    [
        (WebsiteType.Brochure, "A few pages about my business", "£295"),
        (WebsiteType.Business, "Bookings, integrations, dynamic content", "£495"),
        (WebsiteType.Advanced, "Custom system / web-app", "£1,750"),
    ];

    private static readonly (WebsiteAddOn AddOn, string Label, string Price)[] AddOnOptions =
    [
        (WebsiteAddOn.Ecommerce, "E-commerce / online store", "+£600"),
        (WebsiteAddOn.Booking, "Booking & scheduling", "+£300"),
        (WebsiteAddOn.Copywriting, "Copywriting", "+£250"),
        (WebsiteAddOn.ExtraPages, "Extra pages", "+£150"),
        (WebsiteAddOn.Branding, "Branding / logo", "+£300"),
    ];

    private static readonly (CarePlanLevel Level, string Label)[] CareOptions =
    [
        (CarePlanLevel.None, "None"),
        (CarePlanLevel.Basic, "Basic · £10/mo"),
        (CarePlanLevel.Standard, "Standard · £20/mo"),
        (CarePlanLevel.Premium, "Premium · £40/mo"),
    ];

    private static readonly (AutomationScale Scale, string Label, string Price)[] AutomationOptions =
    [
        (AutomationScale.Small, "Automate a few repetitive tasks", "£150"),
        (AutomationScale.Mid, "Connect multiple tools / teams", "£400"),
        (AutomationScale.Enterprise, "Enterprise-grade, audited, at scale", "£900"),
    ];

    // ── Selection state ───────────────────────────────────────────────────
    private bool _needsWebsite = true;
    private WebsiteType? _websiteType;
    private readonly HashSet<WebsiteAddOn> _addOns = [];
    private CarePlanLevel _care = CarePlanLevel.None;

    private bool _needsAutomation;
    private AutomationScale? _automationScale;
    private bool _automationSupport;

    // ── Lead capture ──────────────────────────────────────────────────────
    private readonly LeadModel _lead = new();
    private bool _sending;
    private bool _submitted;

    private QuoteSelection CurrentSelection => new(
        _needsWebsite, _websiteType, _addOns, _care,
        _needsAutomation, _automationScale, _automationSupport);

    private QuoteEstimate Estimate => Estimator.Estimate(CurrentSelection);

    private bool CanSubmit =>
        Estimate.HasEstimate
        && !string.IsNullOrWhiteSpace(_lead.Name)
        && !string.IsNullOrWhiteSpace(_lead.Email);

    // ── Handlers ──────────────────────────────────────────────────────────
    private void ToggleWebsite() => _needsWebsite = !_needsWebsite;
    private void ToggleAutomation() => _needsAutomation = !_needsAutomation;
    private void SelectWebsiteType(WebsiteType t) => _websiteType = t;
    private void SelectCare(CarePlanLevel c) => _care = c;
    private void SelectAutomationScale(AutomationScale s) => _automationScale = s;
    private void ToggleSupport() => _automationSupport = !_automationSupport;
    private void ToggleAddOn(WebsiteAddOn a) { if (!_addOns.Add(a)) _addOns.Remove(a); }

    private async Task SubmitAsync()
    {
        if (!CanSubmit) return;
        _sending = true;
        StateHasChanged();

        var content = QuoteEnquiryFactory.Build(CurrentSelection, Estimate, _lead.Note);
        var enquiry = new ContactEnquiry
        {
            Id = Guid.NewGuid(),
            Name = _lead.Name.Trim(),
            Email = _lead.Email.Trim(),
            Phone = string.IsNullOrWhiteSpace(_lead.Phone) ? null : _lead.Phone.Trim(),
            ServiceType = content.ServiceType,
            Budget = content.Budget,
            Message = content.Message,
            HowHeard = "Quote estimator",
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        try
        {
            await EnquiryService.CreateAsync(enquiry);
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "sending your quote request"));
            _sending = false;
            return;
        }

        try { await EmailService.SendContactEnquiryAsync(enquiry); }
        catch (Exception ex) { Logger.LogError(ex, "Quote estimator notification email failed for {Email}", enquiry.Email); }

        _sending = false;
        _submitted = true;
    }

    // ── UI helpers ────────────────────────────────────────────────────────
    private static string Money(decimal v) => QuotePricing.Money(v);

    private static string ToggleClass(bool active) => active
        ? "rounded-xl border px-4 py-2.5 text-sm text-left transition-colors bg-accent/20 border-accent text-accent"
        : "rounded-xl border px-4 py-2.5 text-sm text-left transition-colors bg-slate-100 dark:bg-white/5 border-slate-200 dark:border-white/10 text-slate-600 dark:text-neutral-400 hover:border-slate-300 dark:hover:border-white/20";

    private sealed class LeadModel
    {
        [Required] public string Name { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Note { get; set; }
    }
}
