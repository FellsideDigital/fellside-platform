using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Analytics;

public partial class Index : ComponentBase
{
    [Inject] private IVisitorAnalyticsService Analytics { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private VisitorAnalyticsSummary? _summary;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _summary = await Analytics.GetSummaryAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "loading visitor analytics"));
            _summary = new VisitorAnalyticsSummary();
        }
    }
}
