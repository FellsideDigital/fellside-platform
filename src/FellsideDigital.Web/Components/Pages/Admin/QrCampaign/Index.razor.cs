using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.QrCampaign;

public partial class Index : ComponentBase
{
    [Inject] private IQrLeadService QrLeadService { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private QrCampaignStats? _stats;
    private List<QrLead>?    _leads;
    private QrLead?          _selected;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _stats = await QrLeadService.GetCampaignStatsAsync();
        _leads = await QrLeadService.GetLeadsAsync();
    }

    private void OpenLead(QrLead lead) => _selected = lead;
    private void CloseDrawer()         => _selected = null;

    private async Task MarkAsRead()
    {
        if (_selected is null) return;

        try
        {
            await QrLeadService.MarkLeadAsReadAsync(_selected.Id);
            _selected.IsRead = true;
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the lead"));
        }
        StateHasChanged();
    }
}
