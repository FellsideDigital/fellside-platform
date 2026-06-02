using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Index : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

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
        try
        {
            await ProjectService.DeleteAsync(_pendingDelete.Id);
            _pendingDelete = null;
            _projects = await ProjectService.GetAllAsync();
        }
        finally
        {
            _deleting = false;
        }
    }
}
