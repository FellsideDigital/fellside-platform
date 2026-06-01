using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class Index : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private PortalPreviewState PreviewState { get; set; } = default!;

    private bool _loading = true;
    private string _firstName = "there";
    private string? _userId;

    private List<ClientProject> _projects = [];
    private List<Invoice>       _invoices = [];
    private List<(ProjectStatusUpdate Update, ClientProject Project)> _recentActivity = [];

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var ownUserId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(ownUserId)) { _loading = false; return; }

        var isSiteAdmin = authState.User.IsInRole("SiteAdmin");
        _userId = PreviewState.ResolveClientId(ownUserId, isSiteAdmin);

        // Resolve the user whose portal is being shown (the previewed client when
        // an admin is previewing, otherwise the logged-in user) for the greeting.
        var user = await UserManager.FindByIdAsync(_userId);
        if (user is not null)
        {
            _firstName = !string.IsNullOrWhiteSpace(user.FirstName) ? user.FirstName
                : user.Email ?? "there";
        }

        _projects = await ProjectService.GetForClientAsync(_userId);
        _invoices = await InvoiceService.GetForClientAsync(_userId);

        _recentActivity = _projects
            .SelectMany(p => p.StatusUpdates.Select(u => (Update: u, Project: p)))
            .OrderByDescending(x => x.Update.CreatedAt)
            .ToList();

        _loading = false;
    }
}
