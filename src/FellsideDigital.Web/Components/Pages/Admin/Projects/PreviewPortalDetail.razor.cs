using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PreviewPortalDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private Dictionary<Guid, string> _downloadUrls = [];

    protected override async Task OnInitializedAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is null)
        {
            NavigationManager.NavigateTo("/Admin/Projects");
            return;
        }

        foreach (var inv in _project.Invoices.Where(i => i.FilePath is not null))
        {
            try { _downloadUrls[inv.Id] = await InvoiceService.GetDownloadUrlAsync(inv.Id) ?? ""; }
            catch { }
        }
    }
}
