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
        WebsiteType.Brochure => 495m,
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
