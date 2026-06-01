using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class PortalPreview : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private PortalPreviewState PreviewState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        // Must run inside the live circuit, not the prerender pass: PreviewState is
        // scoped to the circuit, and navigating during prerender is an HTTP redirect
        // that would discard it. The interactive NavigateTo below stays in-circuit,
        // so the state survives into the portal pages.
        if (!RendererInfo.IsInteractive) return;

        var project = await ProjectService.GetByIdAsync(Id);
        if (project is null)
        {
            NavigationManager.NavigateTo("/Admin/Projects");
            return;
        }

        PreviewState.Enter(project.ClientId, ResolveClientName(project), project.Id);
        NavigationManager.NavigateTo("/Portal");
    }

    private static string ResolveClientName(Data.ClientProject project)
    {
        var client = project.Client;
        if (client is null) return "client";

        var fullName = string.Join(" ", new[] { client.FirstName, client.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        if (!string.IsNullOrWhiteSpace(fullName)) return fullName;
        if (!string.IsNullOrWhiteSpace(client.CompanyName)) return client.CompanyName!;
        return client.Email ?? "client";
    }
}
