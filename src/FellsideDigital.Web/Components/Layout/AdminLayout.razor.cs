using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Services;

namespace FellsideDigital.Web.Components.Layout;

/// <summary>Lightweight project details the admin sidebar needs to render its project-context nav.</summary>
public sealed record ProjectNavContext(Guid Id, string Name, ProjectStatus Status, string? ClientId);

public partial class AdminLayout : LayoutComponentBase, IDisposable
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IServiceScopeFactory ScopeFactory { get; set; } = default!;

    private bool _sidebarOpen;
    private bool _sidebarCollapsed;
    private string _displayName = "";
    private string _initials = "";
    private ProjectNavContext? _project;

    /// <summary>The desktop sidebar is always shown outside a project; inside one it hides while collapsed.</summary>
    private bool ShowDesktopSidebar => _project is null || !_sidebarCollapsed;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = authState.User;

        var first = user.FindFirstValue(ClaimTypes.GivenName) ?? "";
        var last = user.FindFirstValue(ClaimTypes.Surname) ?? "";
        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.Identity?.Name ?? "Admin";

        var fullName = $"{first} {last}".Trim();
        _displayName = string.IsNullOrWhiteSpace(fullName) ? email : fullName;

        _initials = $"{first.FirstOrDefault()}{last.FirstOrDefault()}".ToUpper().Trim();
        if (string.IsNullOrEmpty(_initials))
            _initials = email[0].ToString().ToUpper();

        Navigation.LocationChanged += OnLocationChanged;
        await SyncProjectContextAsync();
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        CloseSidebar();
        await SyncProjectContextAsync();
    }

    /// <summary>
    /// Loads (or clears) the active project when the route targets a specific project, so the
    /// sidebar can switch to its project-context nav. Runs the query on a dedicated DI scope
    /// because the circuit-scoped DbContext is shared with the active page.
    /// </summary>
    private async Task SyncProjectContextAsync()
    {
        var projectId = ResolveProjectId(Navigation.ToBaseRelativePath(Navigation.Uri));

        if (projectId is null)
        {
            _sidebarCollapsed = false;
            if (_project is not null)
            {
                _project = null;
                await InvokeAsync(StateHasChanged);
            }
            return;
        }

        if (_project?.Id == projectId)
        {
            // Navigating between pages of the same project — collapse the nav so the page runs full-width.
            if (!_sidebarCollapsed)
            {
                _sidebarCollapsed = true;
                await InvokeAsync(StateHasChanged);
            }
            return;
        }

        // Entering (or switching to) a project — show the nav first so it stays discoverable.
        _sidebarCollapsed = false;

        await using var scope = ScopeFactory.CreateAsyncScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
        var project = await projectService.GetByIdAsync(projectId.Value);

        _project = project is null
            ? null
            : new ProjectNavContext(project.Id, project.Name, project.Status, project.ClientId);

        await InvokeAsync(StateHasChanged);
    }

    private void ToggleSidebar() => _sidebarCollapsed = !_sidebarCollapsed;

    /// <summary>
    /// Extracts the project id from a relative admin URL. Matches <c>Admin/Projects/{guid}[/...]</c>,
    /// and also <c>Admin/Clients/.../Invoices?from={guid}</c> so managing a project's invoices keeps
    /// the project in context.
    /// </summary>
    internal static Guid? ResolveProjectId(string relativePath)
    {
        var path = relativePath;
        var query = "";
        var queryIndex = relativePath.IndexOf('?');
        if (queryIndex >= 0)
        {
            path = relativePath[..queryIndex];
            query = relativePath[(queryIndex + 1)..];
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 3
            && segments[0].Equals("Admin", StringComparison.OrdinalIgnoreCase)
            && segments[1].Equals("Projects", StringComparison.OrdinalIgnoreCase)
            && Guid.TryParse(segments[2], out var projectId))
        {
            return projectId;
        }

        if (segments.Length >= 4
            && segments[0].Equals("Admin", StringComparison.OrdinalIgnoreCase)
            && segments[1].Equals("Clients", StringComparison.OrdinalIgnoreCase)
            && segments[3].Equals("Invoices", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2
                    && kv[0].Equals("from", StringComparison.OrdinalIgnoreCase)
                    && Guid.TryParse(Uri.UnescapeDataString(kv[1]), out var fromId))
                {
                    return fromId;
                }
            }
        }

        return null;
    }

    private void OpenSidebar() => _sidebarOpen = true;
    private void CloseSidebar() => _sidebarOpen = false;

    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;
}
