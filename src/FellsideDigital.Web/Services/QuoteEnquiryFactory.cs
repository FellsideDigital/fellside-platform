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

        var message = sb.ToString().Replace("\r\n", "\n");
        return new QuoteEnquiryContent(serviceType, budget, message);
    }
}
