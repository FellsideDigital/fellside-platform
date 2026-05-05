using FellsideDigital.UI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace FellsideDigital.Web.Components.Navigation;

public partial class Navbar : ComponentBase, IDisposable
{
    private bool _mobileMenuOpen;
    private bool _hasNotifications = true;

    private string? currentUrl;

    [Parameter] public NavbarLayoutMode LayoutMode { get; set; } = NavbarLayoutMode.Centered;

    private string ContainerClass => LayoutMode == NavbarLayoutMode.Wide
        ? "w-full px-2 sm:px-6 lg:px-8"
        : "mx-auto max-w-7xl px-2 sm:px-6 lg:px-8";

    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private static readonly (string Label, string Href)[] _navLinks =
    {
        ("Home", "/"),
        ("Contact", "/contact")
    };

    private static readonly (string Label, string Href)[] _servicesLinks =
    {
        ("Websites", "/websites"),
        ("Automation", "/automation"),
    };

    private static readonly (string Label, string Href)[] _mobileNavLinks =
    {
        ("Home", "/"),
        ("Websites", "/websites"),
        ("Automation", "/automation"),
        ("Contact", "/contact")
    };

    protected override void OnInitialized()
    {
        currentUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        NavigationManager.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        currentUrl = NavigationManager.ToBaseRelativePath(e.Location);
        StateHasChanged();
    }

    private void ToggleMobileMenu() => _mobileMenuOpen = !_mobileMenuOpen;
    private void CloseMobileMenu() => _mobileMenuOpen = false;

    public void Dispose()
    {
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}
