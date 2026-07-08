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
