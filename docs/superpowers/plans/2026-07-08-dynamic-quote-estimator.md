# Dynamic Quote Estimator Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `/quote` page that asks a few tap-to-answer questions, shows a live estimated price range, and files the request into the existing enquiry inbox.

**Architecture:** A pure, unit-tested `QuoteEstimatorService` turns a `QuoteSelection` into a `QuoteEstimate` (price range + monthly). A pure `QuoteEnquiryFactory` composes the enquiry text. A Blazor Interactive-Server page (`Quote.razor`) drives selection state, renders the live estimate, captures name/email, and submits through the existing `IEnquiryService` + `EmailService` pipeline (no DB/schema change). Three CTAs on `/websites` repoint to `/quote`.

**Tech Stack:** .NET / Blazor Server, xUnit, Tailwind. All prices are plain `decimal`; pricing rules live in one static `QuotePricing` class.

## Global Constraints

- **Build (WSL):** use `dotnet.exe`, not `dotnet`. The `App.razor` `Html` CS0103 error is a flaky source-generator artifact — ignore it if the build otherwise succeeds.
- **Pricing numbers are fixed (approved):** Website base — Brochure £295, Business £495, Advanced £1,750. Add-ons — E-commerce +£600, Booking +£300, Copywriting +£250, Extra pages +£150, Branding +£300. Care /mo — Basic £10, Standard £20, Premium £40. Automation base — Small £150, Mid £400, Enterprise £900. Automation support — from £10/mo. Range high = `ceil((subtotal × 1.40) / 50) × 50`.
- **Money formatting:** always `"£" + value.ToString("N0", CultureInfo.InvariantCulture)` so thousands separators are deterministic in tests.
- **Conventions:** business logic in services (not components); no `ex.Message` to users — wrap risky ops in `try/catch` → `ErrorHandling.LogAndDescribe(Logger, ex, "…")` + `ToastService`; reuse `FieldStyles.MarketingInput`; no new secrets, no raw SQL, no `[Authorize]` (public marketing page).
- **No schema change:** submit reuses `ContactEnquiry` (all columns are `text`).

---

## File Structure

**New**
- `src/FellsideDigital.Domain/Enums/Quote.cs` — `WebsiteType`, `WebsiteAddOn`, `CarePlanLevel`, `AutomationScale` enums (framework-free, alongside `Contact.cs`).
- `src/FellsideDigital.Web/Services/QuoteModels.cs` — `QuoteSelection`, `QuoteEstimate`, `QuoteEnquiryContent` records + static `QuotePricing`.
- `src/FellsideDigital.Web/Services/IQuoteEstimatorService.cs`
- `src/FellsideDigital.Web/Services/QuoteEstimatorService.cs`
- `src/FellsideDigital.Web/Services/QuoteEnquiryFactory.cs` — static composer.
- `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor`
- `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor.cs`
- `tests/FellsideDigital.Tests/QuoteEstimatorServiceTests.cs`
- `tests/FellsideDigital.Tests/QuoteEnquiryFactoryTests.cs`

**Modified**
- `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs` — register `IQuoteEstimatorService`.
- `src/FellsideDigital.Web/wwwroot/sitemap.xml` — add `/quote`.
- `src/FellsideDigital.Web/Components/Pages/Marketing/Websites.razor` — 3 CTA hrefs → `/quote`.

---

## Task 1: Estimator core + pricing

**Files:**
- Create: `src/FellsideDigital.Domain/Enums/Quote.cs`
- Create: `src/FellsideDigital.Web/Services/QuoteModels.cs`
- Create: `src/FellsideDigital.Web/Services/IQuoteEstimatorService.cs`
- Create: `src/FellsideDigital.Web/Services/QuoteEstimatorService.cs`
- Modify: `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs` (after line 108, `AddScoped<IQrLeadService, QrLeadService>()`)
- Test: `tests/FellsideDigital.Tests/QuoteEstimatorServiceTests.cs`

**Interfaces:**
- Produces:
  - `enum WebsiteType { Brochure, Business, Advanced }`
  - `enum WebsiteAddOn { Ecommerce, Booking, Copywriting, ExtraPages, Branding }`
  - `enum CarePlanLevel { None, Basic, Standard, Premium }`
  - `enum AutomationScale { Small, Mid, Enterprise }` (all in `FellsideDigital.Domain.Enums`)
  - `record QuoteSelection(bool NeedsWebsite, WebsiteType? WebsiteType, IReadOnlySet<WebsiteAddOn> AddOns, CarePlanLevel Care, bool NeedsAutomation, AutomationScale? AutomationScale, bool AutomationSupport)`
  - `record QuoteEstimate(decimal OneOffLow, decimal OneOffHigh, decimal MonthlyFrom, bool HasEstimate)`
  - `static class QuotePricing` with `WebsiteBase`, `AddOn`, `Care`, `AutomationBase`, `AutomationSupportFrom`, `UpliftFactor`, and `Label(...)` overloads
  - `interface IQuoteEstimatorService { QuoteEstimate Estimate(QuoteSelection selection); }`
  - `class QuoteEstimatorService : IQuoteEstimatorService`
  (all service types in `FellsideDigital.Web.Services`)

- [ ] **Step 1: Write the enums**

Create `src/FellsideDigital.Domain/Enums/Quote.cs`:

```csharp
namespace FellsideDigital.Domain.Enums;

public enum WebsiteType { Brochure, Business, Advanced }

public enum WebsiteAddOn { Ecommerce, Booking, Copywriting, ExtraPages, Branding }

public enum CarePlanLevel { None, Basic, Standard, Premium }

public enum AutomationScale { Small, Mid, Enterprise }
```

- [ ] **Step 2: Write the models + pricing**

Create `src/FellsideDigital.Web/Services/QuoteModels.cs`:

```csharp
using System.Globalization;
using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Services;

public record QuoteSelection(
    bool NeedsWebsite,
    WebsiteType? WebsiteType,
    IReadOnlySet<WebsiteAddOn> AddOns,
    CarePlanLevel Care,
    bool NeedsAutomation,
    AutomationScale? AutomationScale,
    bool AutomationSupport);

public record QuoteEstimate(
    decimal OneOffLow,
    decimal OneOffHigh,
    decimal MonthlyFrom,
    bool HasEstimate);

public record QuoteEnquiryContent(string ServiceType, string Budget, string Message);

public static class QuotePricing
{
    public const decimal UpliftFactor = 1.40m;
    public const decimal AutomationSupportFrom = 10m;

    public static decimal WebsiteBase(WebsiteType t) => t switch
    {
        WebsiteType.Brochure => 295m,
        WebsiteType.Business => 495m,
        WebsiteType.Advanced => 1750m,
        _ => 0m,
    };

    public static decimal AddOn(WebsiteAddOn a) => a switch
    {
        WebsiteAddOn.Ecommerce => 600m,
        WebsiteAddOn.Booking => 300m,
        WebsiteAddOn.Copywriting => 250m,
        WebsiteAddOn.ExtraPages => 150m,
        WebsiteAddOn.Branding => 300m,
        _ => 0m,
    };

    public static decimal Care(CarePlanLevel c) => c switch
    {
        CarePlanLevel.Basic => 10m,
        CarePlanLevel.Standard => 20m,
        CarePlanLevel.Premium => 40m,
        _ => 0m,
    };

    public static decimal AutomationBase(AutomationScale s) => s switch
    {
        AutomationScale.Small => 150m,
        AutomationScale.Mid => 400m,
        AutomationScale.Enterprise => 900m,
        _ => 0m,
    };

    public static string Label(WebsiteType t) => t switch
    {
        WebsiteType.Brochure => "Brochure site",
        WebsiteType.Business => "Business site",
        WebsiteType.Advanced => "Advanced / custom build",
        _ => t.ToString(),
    };

    public static string Label(WebsiteAddOn a) => a switch
    {
        WebsiteAddOn.Ecommerce => "E-commerce",
        WebsiteAddOn.Booking => "Booking & scheduling",
        WebsiteAddOn.Copywriting => "Copywriting",
        WebsiteAddOn.ExtraPages => "Extra pages",
        WebsiteAddOn.Branding => "Branding / logo",
        _ => a.ToString(),
    };

    public static string Label(CarePlanLevel c) => c switch
    {
        CarePlanLevel.Basic => "Basic",
        CarePlanLevel.Standard => "Standard",
        CarePlanLevel.Premium => "Premium",
        _ => "None",
    };

    public static string Label(AutomationScale s) => s switch
    {
        AutomationScale.Small => "Small Business",
        AutomationScale.Mid => "Mid-Market",
        AutomationScale.Enterprise => "Enterprise",
        _ => s.ToString(),
    };

    public static string Money(decimal v) =>
        "£" + v.ToString("N0", CultureInfo.InvariantCulture);
}
```

- [ ] **Step 3: Write the interface**

Create `src/FellsideDigital.Web/Services/IQuoteEstimatorService.cs`:

```csharp
namespace FellsideDigital.Web.Services;

public interface IQuoteEstimatorService
{
    QuoteEstimate Estimate(QuoteSelection selection);
}
```

- [ ] **Step 4: Write the failing tests**

Create `tests/FellsideDigital.Tests/QuoteEstimatorServiceTests.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class QuoteEstimatorServiceTests
{
    private readonly QuoteEstimatorService _sut = new();

    private static QuoteSelection Sel(
        bool needsWebsite = false, WebsiteType? websiteType = null,
        WebsiteAddOn[]? addOns = null, CarePlanLevel care = CarePlanLevel.None,
        bool needsAutomation = false, AutomationScale? scale = null, bool support = false)
        => new(needsWebsite, websiteType,
            new HashSet<WebsiteAddOn>(addOns ?? Array.Empty<WebsiteAddOn>()),
            care, needsAutomation, scale, support);

    [Fact]
    public void No_selection_has_no_estimate()
    {
        var e = _sut.Estimate(Sel());
        Assert.False(e.HasEstimate);
        Assert.Equal(0m, e.OneOffLow);
        Assert.Equal(0m, e.OneOffHigh);
    }

    [Fact]
    public void Business_site_only_produces_base_to_uplifted_range()
    {
        var e = _sut.Estimate(Sel(needsWebsite: true, websiteType: WebsiteType.Business));
        Assert.True(e.HasEstimate);
        Assert.Equal(495m, e.OneOffLow);
        Assert.Equal(700m, e.OneOffHigh); // ceil(495*1.4/50)*50 = ceil(693/50)*50
        Assert.Equal(0m, e.MonthlyFrom);
    }

    [Fact]
    public void Add_ons_and_care_are_included()
    {
        var e = _sut.Estimate(Sel(
            needsWebsite: true, websiteType: WebsiteType.Business,
            addOns: new[] { WebsiteAddOn.Ecommerce, WebsiteAddOn.Copywriting },
            care: CarePlanLevel.Standard));
        Assert.Equal(1345m, e.OneOffLow);   // 495 + 600 + 250
        Assert.Equal(1900m, e.OneOffHigh);  // ceil(1345*1.4/50)*50 = ceil(1883/50)*50
        Assert.Equal(20m, e.MonthlyFrom);
    }

    [Fact]
    public void Automation_only_produces_range()
    {
        var e = _sut.Estimate(Sel(needsAutomation: true, scale: AutomationScale.Small));
        Assert.True(e.HasEstimate);
        Assert.Equal(150m, e.OneOffLow);
        Assert.Equal(250m, e.OneOffHigh); // ceil(210/50)*50
    }

    [Fact]
    public void Automation_support_adds_monthly()
    {
        var e = _sut.Estimate(Sel(needsAutomation: true, scale: AutomationScale.Small, support: true));
        Assert.Equal(10m, e.MonthlyFrom);
    }

    [Fact]
    public void Website_and_automation_combine()
    {
        var e = _sut.Estimate(Sel(
            needsWebsite: true, websiteType: WebsiteType.Business,
            needsAutomation: true, scale: AutomationScale.Small));
        Assert.Equal(645m, e.OneOffLow);   // 495 + 150
        Assert.Equal(950m, e.OneOffHigh);  // ceil(903/50)*50
    }

    [Fact]
    public void Add_ons_ignored_when_website_type_not_chosen()
    {
        var e = _sut.Estimate(Sel(
            needsWebsite: true, websiteType: null,
            addOns: new[] { WebsiteAddOn.Ecommerce },
            needsAutomation: true, scale: AutomationScale.Small));
        Assert.Equal(150m, e.OneOffLow); // only automation counts
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet.exe test tests/FellsideDigital.Tests --filter "FullyQualifiedName~QuoteEstimatorServiceTests"`
Expected: FAIL to compile — `QuoteEstimatorService` does not exist yet.

- [ ] **Step 6: Write the service**

Create `src/FellsideDigital.Web/Services/QuoteEstimatorService.cs`:

```csharp
namespace FellsideDigital.Web.Services;

public class QuoteEstimatorService : IQuoteEstimatorService
{
    public QuoteEstimate Estimate(QuoteSelection s)
    {
        bool hasWebsite = s.NeedsWebsite && s.WebsiteType.HasValue;
        bool hasAutomation = s.NeedsAutomation && s.AutomationScale.HasValue;

        if (!hasWebsite && !hasAutomation)
            return new QuoteEstimate(0m, 0m, 0m, false);

        decimal subtotal = 0m;
        if (hasWebsite)
        {
            subtotal += QuotePricing.WebsiteBase(s.WebsiteType!.Value);
            foreach (var a in s.AddOns) subtotal += QuotePricing.AddOn(a);
        }
        if (hasAutomation)
            subtotal += QuotePricing.AutomationBase(s.AutomationScale!.Value);

        decimal high = RoundUpTo50(subtotal * QuotePricing.UpliftFactor);

        decimal monthly = 0m;
        if (hasWebsite) monthly += QuotePricing.Care(s.Care);
        if (hasAutomation && s.AutomationSupport) monthly += QuotePricing.AutomationSupportFrom;

        return new QuoteEstimate(subtotal, high, monthly, true);
    }

    private static decimal RoundUpTo50(decimal x) => Math.Ceiling(x / 50m) * 50m;
}
```

- [ ] **Step 7: Register the service**

In `src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs`, immediately after the line `services.AddScoped<IQrLeadService, QrLeadService>();` add:

```csharp
        services.AddSingleton<IQuoteEstimatorService, QuoteEstimatorService>();
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet.exe test tests/FellsideDigital.Tests --filter "FullyQualifiedName~QuoteEstimatorServiceTests"`
Expected: PASS (7 tests).

- [ ] **Step 9: Commit**

```bash
git add src/FellsideDigital.Domain/Enums/Quote.cs \
        src/FellsideDigital.Web/Services/QuoteModels.cs \
        src/FellsideDigital.Web/Services/IQuoteEstimatorService.cs \
        src/FellsideDigital.Web/Services/QuoteEstimatorService.cs \
        src/FellsideDigital.Web/Extensions/ServiceConfigurationExtensions.cs \
        tests/FellsideDigital.Tests/QuoteEstimatorServiceTests.cs
git commit -m "feat: quote estimator service + pricing"
```

---

## Task 2: Enquiry composition helper

**Files:**
- Create: `src/FellsideDigital.Web/Services/QuoteEnquiryFactory.cs`
- Test: `tests/FellsideDigital.Tests/QuoteEnquiryFactoryTests.cs`

**Interfaces:**
- Consumes: `QuoteSelection`, `QuoteEstimate`, `QuotePricing` (Task 1).
- Produces: `static class QuoteEnquiryFactory { static QuoteEnquiryContent Build(QuoteSelection selection, QuoteEstimate estimate, string? note); }`

- [ ] **Step 1: Write the failing tests**

Create `tests/FellsideDigital.Tests/QuoteEnquiryFactoryTests.cs`:

```csharp
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Tests;

public class QuoteEnquiryFactoryTests
{
    private static QuoteSelection Sel(
        bool needsWebsite = false, WebsiteType? websiteType = null,
        WebsiteAddOn[]? addOns = null, CarePlanLevel care = CarePlanLevel.None,
        bool needsAutomation = false, AutomationScale? scale = null, bool support = false)
        => new(needsWebsite, websiteType,
            new HashSet<WebsiteAddOn>(addOns ?? Array.Empty<WebsiteAddOn>()),
            care, needsAutomation, scale, support);

    [Fact]
    public void ServiceType_reflects_selected_streams()
    {
        var both = Sel(needsWebsite: true, websiteType: WebsiteType.Business,
                       needsAutomation: true, scale: AutomationScale.Small);
        var estimate = new QuoteEstimate(645m, 950m, 0m, true);

        var content = QuoteEnquiryFactory.Build(both, estimate, null);

        Assert.Equal("Website + Automation", content.ServiceType);
    }

    [Fact]
    public void Website_only_service_type()
    {
        var s = Sel(needsWebsite: true, websiteType: WebsiteType.Brochure);
        var content = QuoteEnquiryFactory.Build(s, new QuoteEstimate(295m, 450m, 0m, true), null);
        Assert.Equal("Website", content.ServiceType);
    }

    [Fact]
    public void Budget_carries_the_estimated_range()
    {
        var s = Sel(needsWebsite: true, websiteType: WebsiteType.Business);
        var content = QuoteEnquiryFactory.Build(s, new QuoteEstimate(1245m, 1750m, 30m, true), null);
        Assert.Equal("Est. £1,245–£1,750 (estimator)", content.Budget);
    }

    [Fact]
    public void Message_includes_selections_estimate_and_note()
    {
        var s = Sel(
            needsWebsite: true, websiteType: WebsiteType.Business,
            addOns: new[] { WebsiteAddOn.Ecommerce }, care: CarePlanLevel.Standard,
            needsAutomation: true, scale: AutomationScale.Small, support: true);
        var estimate = new QuoteEstimate(1245m, 1750m, 30m, true);

        var content = QuoteEnquiryFactory.Build(s, estimate, "Please call me in the morning.");

        Assert.Contains("Business site (£495)", content.Message);
        Assert.Contains("E-commerce", content.Message);
        Assert.Contains("Standard (£20/mo)", content.Message);
        Assert.Contains("Small Business (£150)", content.Message);
        Assert.Contains("ongoing support", content.Message);
        Assert.Contains("£1,245 – £1,750", content.Message);
        Assert.Contains("from £30/mo", content.Message);
        Assert.Contains("Please call me in the morning.", content.Message);
    }

    [Fact]
    public void Missing_note_renders_dash()
    {
        var s = Sel(needsWebsite: true, websiteType: WebsiteType.Brochure);
        var content = QuoteEnquiryFactory.Build(s, new QuoteEstimate(295m, 450m, 0m, true), "  ");
        Assert.Contains("Their note:\n—", content.Message);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet.exe test tests/FellsideDigital.Tests --filter "FullyQualifiedName~QuoteEnquiryFactoryTests"`
Expected: FAIL to compile — `QuoteEnquiryFactory` does not exist.

- [ ] **Step 3: Write the factory**

Create `src/FellsideDigital.Web/Services/QuoteEnquiryFactory.cs`:

```csharp
using System.Text;
using FellsideDigital.Domain.Enums;

namespace FellsideDigital.Web.Services;

public static class QuoteEnquiryFactory
{
    public static QuoteEnquiryContent Build(QuoteSelection s, QuoteEstimate e, string? note)
    {
        bool hasWebsite = s.NeedsWebsite && s.WebsiteType.HasValue;
        bool hasAutomation = s.NeedsAutomation && s.AutomationScale.HasValue;

        var serviceType = (hasWebsite, hasAutomation) switch
        {
            (true, true) => "Website + Automation",
            (true, false) => "Website",
            (false, true) => "Automation",
            _ => "Quote request",
        };

        var budget = $"Est. {QuotePricing.Money(e.OneOffLow)}–{QuotePricing.Money(e.OneOffHigh)} (estimator)";

        var sb = new StringBuilder();
        sb.AppendLine("--- Quote estimator ---");

        if (hasWebsite)
        {
            var wt = s.WebsiteType!.Value;
            sb.AppendLine($"Website: {QuotePricing.Label(wt)} ({QuotePricing.Money(QuotePricing.WebsiteBase(wt))})");
            if (s.AddOns.Count > 0)
                sb.AppendLine("  Add-ons: " + string.Join(", ", s.AddOns.Select(QuotePricing.Label)));
            if (s.Care != CarePlanLevel.None)
                sb.AppendLine($"  Care plan: {QuotePricing.Label(s.Care)} ({QuotePricing.Money(QuotePricing.Care(s.Care))}/mo)");
        }

        if (hasAutomation)
        {
            var sc = s.AutomationScale!.Value;
            var support = s.AutomationSupport ? " + ongoing support" : "";
            sb.AppendLine($"Automation: {QuotePricing.Label(sc)} ({QuotePricing.Money(QuotePricing.AutomationBase(sc))}){support}");
        }

        sb.AppendLine($"Estimated one-off: {QuotePricing.Money(e.OneOffLow)} – {QuotePricing.Money(e.OneOffHigh)}");
        if (e.MonthlyFrom > 0m)
            sb.AppendLine($"Estimated monthly: from {QuotePricing.Money(e.MonthlyFrom)}/mo");

        sb.AppendLine();
        sb.AppendLine("Their note:");
        sb.Append(string.IsNullOrWhiteSpace(note) ? "—" : note.Trim());

        return new QuoteEnquiryContent(serviceType, budget, sb.ToString());
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet.exe test tests/FellsideDigital.Tests --filter "FullyQualifiedName~QuoteEnquiryFactoryTests"`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add src/FellsideDigital.Web/Services/QuoteEnquiryFactory.cs \
        tests/FellsideDigital.Tests/QuoteEnquiryFactoryTests.cs
git commit -m "feat: compose quote enquiry content from selection + estimate"
```

---

## Task 3: `/quote` page

**Files:**
- Create: `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor`
- Create: `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor.cs`
- Modify: `src/FellsideDigital.Web/wwwroot/sitemap.xml`

**Interfaces:**
- Consumes: `IQuoteEstimatorService` (Task 1), `QuoteEnquiryFactory` (Task 2), `QuotePricing.Money`, `IEnquiryService`, `IEmailService`, `ToastService`, `ErrorHandling`, `ContactEnquiry`.
- Produces: route `/quote`.

- [ ] **Step 1: Write the code-behind**

Create `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
```

- [ ] **Step 2: Write the Razor page**

Create `src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor`:

```razor
@page "/quote"
@using FellsideDigital.Domain.Enums

<PageTitle>Get an Instant Website & Automation Quote | Fellside Digital</PageTitle>

<SeoHead Title="Get an Instant Website & Automation Quote | Fellside Digital"
         Description="Build your project and get an instant estimated price for a bespoke website or automation from Fellside Digital, Cumbria. No obligation — final fixed price confirmed after a quick chat."
         CanonicalUrl="https://fellsidedigital.co.uk/quote" />

<HeadContent>
    <script type="application/ld+json">
    {
      "@@context": "https://schema.org",
      "@@type": "ContactPage",
      "name": "Get a Quote — Fellside Digital",
      "description": "Instant estimated pricing for bespoke websites and automation from Fellside Digital, Cumbria.",
      "url": "https://fellsidedigital.co.uk/quote",
      "mainEntity": { "@@id": "https://fellsidedigital.co.uk/#business" }
    }
    </script>
    <script type="application/ld+json">
    {
      "@@context": "https://schema.org",
      "@@type": "BreadcrumbList",
      "itemListElement": [
        { "@@type": "ListItem", "position": 1, "name": "Home", "item": "https://fellsidedigital.co.uk/" },
        { "@@type": "ListItem", "position": 2, "name": "Get a quote", "item": "https://fellsidedigital.co.uk/quote" }
      ]
    }
    </script>
</HeadContent>

<div class="bg-white dark:bg-neutral-950 text-slate-900 dark:text-white transition-colors duration-300">

    <!-- HERO -->
    <section class="relative overflow-hidden py-24 sm:py-32 px-6 text-center">
        <div class="hero-aura" aria-hidden="true"><div class="hero-aura-blob"></div></div>
        <div class="max-w-2xl mx-auto space-y-5">
            <p class="text-sm font-medium uppercase tracking-widest text-slate-400 dark:text-neutral-500">Get a quote</p>
            <h1 class="text-4xl sm:text-5xl font-semibold leading-tight">Build your project, see the price.</h1>
            <p class="text-base sm:text-lg text-slate-500 dark:text-neutral-400">
                Answer a few quick questions and get an instant estimate. No obligation —
                your final fixed price is confirmed after a quick chat.
            </p>
        </div>
    </section>

    @if (!_submitted)
    {
        <section class="pb-28 px-6">
            <div class="max-w-5xl mx-auto grid lg:grid-cols-[1fr_20rem] gap-8 items-start">

                <!-- LEFT: builder -->
                <div class="space-y-10">

                    <!-- Website -->
                    <div class="space-y-5">
                        <button type="button" @onclick="ToggleWebsite" class="@ToggleClass(_needsWebsite) w-full sm:w-auto font-semibold">
                            @(_needsWebsite ? "✓ " : "")I need a website
                        </button>

                        @if (_needsWebsite)
                        {
                            <div class="space-y-4 pl-1">
                                <div>
                                    <label class="block text-xs font-semibold uppercase tracking-widest mb-2 text-slate-400 dark:text-neutral-500">What kind of site?</label>
                                    <div class="grid sm:grid-cols-3 gap-2">
                                        @foreach (var o in WebsiteTypeOptions)
                                        {
                                            <button type="button" @onclick="() => SelectWebsiteType(o.Type)" class="@ToggleClass(_websiteType == o.Type)">
                                                <span class="block font-medium">@o.Label</span>
                                                <span class="block text-xs opacity-70 mt-0.5">from @o.Price</span>
                                            </button>
                                        }
                                    </div>
                                </div>

                                <div>
                                    <label class="block text-xs font-semibold uppercase tracking-widest mb-2 text-slate-400 dark:text-neutral-500">Add-ons</label>
                                    <div class="grid sm:grid-cols-2 gap-2">
                                        @foreach (var o in AddOnOptions)
                                        {
                                            <button type="button" @onclick="() => ToggleAddOn(o.AddOn)" class="@ToggleClass(_addOns.Contains(o.AddOn))">
                                                @o.Label <span class="opacity-70">@o.Price</span>
                                            </button>
                                        }
                                    </div>
                                </div>

                                <div>
                                    <label class="block text-xs font-semibold uppercase tracking-widest mb-2 text-slate-400 dark:text-neutral-500">Ongoing care?</label>
                                    <div class="grid grid-cols-2 sm:grid-cols-4 gap-2">
                                        @foreach (var o in CareOptions)
                                        {
                                            <button type="button" @onclick="() => SelectCare(o.Level)" class="@ToggleClass(_care == o.Level)">@o.Label</button>
                                        }
                                    </div>
                                </div>
                            </div>
                        }
                    </div>

                    <!-- Automation -->
                    <div class="space-y-5">
                        <button type="button" @onclick="ToggleAutomation" class="@ToggleClass(_needsAutomation) w-full sm:w-auto font-semibold">
                            @(_needsAutomation ? "✓ " : "")I need automation
                        </button>

                        @if (_needsAutomation)
                        {
                            <div class="space-y-4 pl-1">
                                <div>
                                    <label class="block text-xs font-semibold uppercase tracking-widest mb-2 text-slate-400 dark:text-neutral-500">What scale?</label>
                                    <div class="grid sm:grid-cols-3 gap-2">
                                        @foreach (var o in AutomationOptions)
                                        {
                                            <button type="button" @onclick="() => SelectAutomationScale(o.Scale)" class="@ToggleClass(_automationScale == o.Scale)">
                                                <span class="block font-medium">@o.Label</span>
                                                <span class="block text-xs opacity-70 mt-0.5">from @o.Price</span>
                                            </button>
                                        }
                                    </div>
                                </div>
                                <button type="button" @onclick="ToggleSupport" class="@ToggleClass(_automationSupport)">
                                    @(_automationSupport ? "✓ " : "")Add ongoing support (from £10/mo)
                                </button>
                            </div>
                        }
                    </div>

                    <!-- Capture -->
                    <div class="space-y-4 border-t border-slate-100 dark:border-white/5 pt-8">
                        <h2 class="text-lg font-semibold">Where should we send your detailed quote?</h2>
                        <div class="grid sm:grid-cols-2 gap-4">
                            <input @bind="_lead.Name" @bind:event="oninput" type="text" placeholder="Your name *" class="@FieldStyles.MarketingInput" />
                            <input @bind="_lead.Email" @bind:event="oninput" type="email" placeholder="Email *" class="@FieldStyles.MarketingInput" />
                        </div>
                        <input @bind="_lead.Phone" type="tel" placeholder="Phone (optional)" class="@FieldStyles.MarketingInput" />
                        <textarea @bind="_lead.Note" rows="3" placeholder="Anything else we should know? (optional)" class="@FieldStyles.MarketingInput resize-none"></textarea>
                    </div>
                </div>

                <!-- RIGHT: live estimate -->
                <div class="lg:sticky lg:top-24 rounded-2xl border border-slate-200 dark:border-white/10 bg-slate-50 dark:bg-white/5 p-6 space-y-4">
                    <p class="text-xs font-semibold uppercase tracking-widest text-slate-400 dark:text-neutral-500">Estimated project cost</p>

                    @{ var est = Estimate; }
                    @if (est.HasEstimate)
                    {
                        <p class="text-3xl font-bold text-slate-900 dark:text-white">@Money(est.OneOffLow) – @Money(est.OneOffHigh)</p>
                        <p class="text-xs text-slate-400 dark:text-neutral-500">one-off</p>
                        @if (est.MonthlyFrom > 0)
                        {
                            <p class="text-sm font-medium text-accent">+ from @Money(est.MonthlyFrom)/mo</p>
                        }
                        <p class="text-xs text-slate-500 dark:text-neutral-400 leading-relaxed border-t border-slate-200 dark:border-white/10 pt-3">
                            An indicative estimate — your final fixed price is confirmed after a quick chat.
                        </p>
                    }
                    else
                    {
                        <p class="text-sm text-slate-500 dark:text-neutral-400">Pick what you need to see your estimate.</p>
                    }

                    <button type="button" @onclick="SubmitAsync" disabled="@(!CanSubmit || _sending)"
                            class="w-full rounded-xl bg-accent px-6 py-3 text-sm font-semibold text-white hover:opacity-90 transition-opacity disabled:opacity-40 disabled:cursor-not-allowed">
                        @(_sending ? "Sending…" : "Get my detailed quote →")
                    </button>
                    <p class="text-center text-xs text-slate-400 dark:text-neutral-500">
                        Prefer to talk? <a href="tel:+447484323505" class="text-accent hover:underline">Call</a>
                        or <a href="https://wa.me/447484323505" target="_blank" class="text-accent hover:underline">WhatsApp</a>.
                    </p>
                </div>
            </div>
        </section>
    }
    else
    {
        <section class="pb-32 px-6">
            <div class="max-w-xl mx-auto rounded-2xl border border-green-200 dark:border-green-500/20 bg-green-50 dark:bg-green-500/5 p-8 text-center space-y-3">
                <div class="flex justify-center mb-2">
                    <div class="size-12 rounded-full bg-green-100 dark:bg-green-500/10 flex items-center justify-center">
                        <svg class="size-6 text-green-600 dark:text-green-400" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" d="m4.5 12.75 6 6 9-13.5" />
                        </svg>
                    </div>
                </div>
                <h2 class="text-xl font-bold">Thanks — that's on its way to me.</h2>
                <p class="text-sm text-slate-500 dark:text-neutral-400">
                    I'll come back within one working day with your detailed quote. Your estimate was
                    <strong class="text-slate-900 dark:text-white">@Money(Estimate.OneOffLow) – @Money(Estimate.OneOffHigh)</strong>.
                </p>
            </div>
        </section>
    }
</div>
```

- [ ] **Step 3: Add `/quote` to the sitemap**

In `src/FellsideDigital.Web/wwwroot/sitemap.xml`, add a `<url>` entry directly before the `/contact` entry (which currently starts at line 45):

```xml
  <url>
    <loc>https://fellsidedigital.co.uk/quote</loc>
    <lastmod>2026-07-08</lastmod>
    <changefreq>monthly</changefreq>
    <priority>0.8</priority>
  </url>
```

- [ ] **Step 4: Build to verify the page compiles**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeds (ignore any `App.razor` `Html` CS0103 flake per Global Constraints).

- [ ] **Step 5: Manually verify in the running app**

Start the app (`dotnet.exe run --launch-profile http` or the VS Docker profile) and open `http://localhost:5185/quote` (or `:8080`). Confirm:
- Selecting a site type updates the estimate range live.
- Add-ons and care change the numbers; automation adds to the total.
- "Get my detailed quote" is disabled until a base option + name + email are present.
- Submitting shows the success panel; the enquiry appears in the admin enquiries list with the composed breakdown.

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor \
        src/FellsideDigital.Web/Components/Pages/Marketing/Quote.razor.cs \
        src/FellsideDigital.Web/wwwroot/sitemap.xml
git commit -m "feat: dynamic /quote estimator page"
```

---

## Task 4: Repoint `/websites` CTAs to `/quote`

**Files:**
- Modify: `src/FellsideDigital.Web/Components/Pages/Marketing/Websites.razor`

**Interfaces:**
- Consumes: `/quote` route (Task 3).

- [ ] **Step 1: Repoint the pricing-tier "Get a quote" button**

In `Websites.razor`, the pricing-tier card CTA (around line 204). Change its `href` from `/contact` to `/quote`. It is the `<a>` whose visible text is `Get a quote` inside the tier `@foreach`. Replace:

```razor
                            <a href="/contact"
                               class="mt-6 rounded-xl px-5 py-2.5 text-sm font-semibold text-center transition-all
```

with:

```razor
                            <a href="/quote"
                               class="mt-6 rounded-xl px-5 py-2.5 text-sm font-semibold text-center transition-all
```

- [ ] **Step 2: Repoint the "Build my package" button**

The flagship-bundle CTA (around line 260), visible text `Build my package`. Replace:

```razor
                <a href="/contact"
                   class="flex-shrink-0 rounded-xl bg-accent px-6 py-3 text-sm font-semibold
                          text-white shadow-sm hover:opacity-90 transition-all text-center">
                    Build my package
```

with:

```razor
                <a href="/quote"
                   class="flex-shrink-0 rounded-xl bg-accent px-6 py-3 text-sm font-semibold
                          text-white shadow-sm hover:opacity-90 transition-all text-center">
                    Build my package
```

- [ ] **Step 3: Repoint the bottom "Get a quote" button**

The bottom CTA (around line 470), visible text `Get a quote`. Replace:

```razor
                <a href="/contact"
                   class="rounded-xl bg-accent px-7 py-3 text-sm font-semibold
                          text-white shadow-lg hover:opacity-90 transition-all">
                    Get a quote
```

with:

```razor
                <a href="/quote"
                   class="rounded-xl bg-accent px-7 py-3 text-sm font-semibold
                          text-white shadow-lg hover:opacity-90 transition-all">
                    Get a quote
```

- [ ] **Step 4: Verify only the intended CTAs changed**

Run: `grep -nE "href=\"/quote\"|href=\"/contact\"" src/FellsideDigital.Web/Components/Pages/Marketing/Websites.razor`
Expected: three `/quote` hrefs (the two "Get a quote" and "Build my package") and the hero "Start your project" still on `/contact`.

- [ ] **Step 5: Build**

Run: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj`
Expected: Build succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/FellsideDigital.Web/Components/Pages/Marketing/Websites.razor
git commit -m "feat: point /websites quote & package CTAs at /quote"
```

---

## Final verification

- [ ] Run the full new-test set: `dotnet.exe test tests/FellsideDigital.Tests --filter "FullyQualifiedName~Quote"` → all pass.
- [ ] Build: `dotnet.exe build src/FellsideDigital.Web/FellsideDigital.Web.csproj` → succeeds.
- [ ] Manual smoke of `/quote` per Task 3 Step 4.
