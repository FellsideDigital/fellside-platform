using System.Security.Claims;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FellsideDigital.Web.Components.Layout;

public partial class PortalLayout : LayoutComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private PortalPreviewState PreviewState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;

    private bool _sidebarOpen;
    private string _displayName = "";
    private string _initials = "";
    private List<ClientProject>? _projects;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = authState.User;

        var first = user.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var last = user.FindFirstValue(ClaimTypes.Surname) ?? "";
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? "Client";

        var fullName = $"{first} {last}".Trim();
        _displayName = string.IsNullOrWhiteSpace(fullName) ? email : fullName;

        _initials = $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper().Trim();
        if (string.IsNullOrEmpty(_initials))
            _initials = email[0].ToString().ToUpper();

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
        {
            var clientId = PreviewState.ResolveClientId(userId, user.IsInRole("SiteAdmin"));

            // Resolve the project service in its own DI scope so the sidebar query
            // runs on a dedicated DbContext. The circuit-scoped context is shared
            // with the active page, and two concurrent operations on one DbContext
            // throw "a second operation was started on this context instance".
            await using var scope = ScopeFactory.CreateAsyncScope();
            var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
            _projects = await projectService.GetForClientAsync(clientId);
        }
    }

    private void OpenSidebar() => _sidebarOpen = true;
    private void CloseSidebar() => _sidebarOpen = false;

    private void ExitPreview()
    {
        var sourceProjectId = PreviewState.SourceProjectId;
        PreviewState.Exit();
        NavigationManager.NavigateTo(
            sourceProjectId is { } id ? $"/Admin/Projects/{id}" : "/Admin/Projects");
    }
}
