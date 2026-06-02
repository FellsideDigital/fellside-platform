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
    [Inject] private IProjectDocumentService DocumentService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private PortalPreviewState PreviewState { get; set; } = default!;

    private ClientProject? _project;
    private bool _notFound;
    private Dictionary<Guid, string> _downloadUrls = [];
    private Dictionary<Guid, string> _documentUrls = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = await UserManager.GetUserAsync(authState.User);
        if (user is null) { _notFound = true; return; }

        var clientId = PreviewState.ResolveClientId(user.Id, authState.User.IsInRole("SiteAdmin"));

        // Client-safe load: only client-visible timeline events are included, never internal ones.
        _project = await ProjectService.GetByIdForClientAsync(Id);
        if (_project is null || _project.ClientId != clientId)
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

        // Load presigned S3 download URLs for all shared documents
        _documentUrls = [];
        foreach (var doc in _project.Documents)
        {
            try { _documentUrls[doc.Id] = await DocumentService.GetDownloadUrlAsync(doc.Id) ?? ""; }
            catch { /* non-fatal */ }
        }
    }
}
