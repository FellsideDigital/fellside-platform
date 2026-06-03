using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Index : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private List<ClientProject>? _projects;

    private ClientProject? _pendingDelete;
    private bool _deleting;

    protected override async Task OnInitializedAsync()
    {
        _projects = await ProjectService.GetAllAsync();
    }

    private async Task ConfirmDeleteAsync()
    {
        if (_pendingDelete is null) return;
        _deleting = true;
        var name = _pendingDelete.Name;
        try
        {
            await ProjectService.DeleteAsync(_pendingDelete.Id);
            _pendingDelete = null;
            _projects = await ProjectService.GetAllAsync();
            Toasts.Success($"Project \"{name}\" deleted.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the project"));
        }
        finally
        {
            _deleting = false;
        }
    }
}
