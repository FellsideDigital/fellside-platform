using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace FellsideDigital.Web.Components.Layout;

public partial class PortalLayout : LayoutComponentBase
{
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;

    private bool _sidebarOpen;
    private string _displayName = "";
    private string _initials = "";

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
    }

    private void OpenSidebar() => _sidebarOpen = true;
    private void CloseSidebar() => _sidebarOpen = false;
}
