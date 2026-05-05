namespace FellsideDigital.Web.Services;

public class LayoutStateService
{
    private bool _showNavbar = true;

    public bool ShowNavbar => _showNavbar;

    public event Action? OnChange;

    public void SetNavbar(bool visible)
    {
        if (_showNavbar == visible) return;
        _showNavbar = visible;
        OnChange?.Invoke();
    }
}
