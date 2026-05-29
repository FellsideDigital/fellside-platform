using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PreviewPortalProjects : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private List<ClientProject>? _projects;

    protected override async Task OnInitializedAsync()
    {
        var project = await ProjectService.GetByIdAsync(Id);
        if (project is null)
        {
            NavigationManager.NavigateTo("/Admin/Projects");
            return;
        }
        _projects = await ProjectService.GetForClientAsync(project.ClientId);
    }
}
