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
