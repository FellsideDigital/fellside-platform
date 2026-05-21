using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Components.Pages.Admin.QrCampaign;

public partial class Index : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;

    private QrCampaignStats? _stats;
    private List<QrLead>?    _leads;
    private QrLead?          _selected;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        var scans = await Db.QrScans.ToListAsync();
        var leads = await Db.QrLeads.OrderByDescending(l => l.SubmittedAt).ToListAsync();

        _stats = new QrCampaignStats
        {
            TotalScans = scans.Count,
            ShirtScans = scans.Count(s => s.Source == "shirt"),
            CardScans  = scans.Count(s => s.Source == "card"),
            TotalLeads = leads.Count,
        };

        _leads = leads;
    }

    private void OpenLead(QrLead lead) => _selected = lead;
    private void CloseDrawer()         => _selected = null;

    private async Task MarkAsRead()
    {
        if (_selected is null) return;

        var entity = await Db.QrLeads.FindAsync(_selected.Id);
        if (entity is not null)
        {
            entity.IsRead = true;
            await Db.SaveChangesAsync();
        }

        _selected.IsRead = true;
        StateHasChanged();
    }

    private sealed record QrCampaignStats
    {
        public int TotalScans { get; init; }
        public int ShirtScans { get; init; }
        public int CardScans  { get; init; }
        public int TotalLeads { get; init; }
    }
}
