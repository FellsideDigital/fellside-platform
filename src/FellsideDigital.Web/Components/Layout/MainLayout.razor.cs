using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Layout;

public partial class MainLayout : LayoutComponentBase, IDisposable
{
    [Inject] private LayoutStateService LayoutState { get; set; } = default!;

    protected override void OnInitialized()
    {
        LayoutState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        LayoutState.OnChange -= StateHasChanged;
    }
}
