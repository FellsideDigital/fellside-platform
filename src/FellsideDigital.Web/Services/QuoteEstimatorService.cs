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
