using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class ProjectDetail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private bool _notFound;
    private Dictionary<Guid, string> _downloadUrls = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = await UserManager.GetUserAsync(authState.User);
        if (user is null) { _notFound = true; return; }

        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is null || _project.ClientId != user.Id)
        {
            _notFound = true;
            _project = null;
            return;
        }

        // Load presigned S3 download URLs for all invoices that have a file
        _downloadUrls = [];
        foreach (var inv in _project.Invoices.Where(i => i.FilePath is not null))
        {
            try { _downloadUrls[inv.Id] = await InvoiceService.GetDownloadUrlAsync(inv.Id) ?? ""; }
            catch { /* non-fatal */ }
        }
    }
}
