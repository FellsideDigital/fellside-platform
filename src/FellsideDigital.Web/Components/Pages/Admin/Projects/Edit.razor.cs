using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Edit : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private InputModel Input { get; set; } = new();
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-indigo-400 dark:focus:ring-orange-400 transition-shadow outline-none";

    protected override async Task OnInitializedAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is not null)
        {
            Input.Name = _project.Name;
            Input.Description = _project.Description;
            Input.Type = _project.Type;
            Input.Status = _project.Status;
            Input.PreviewUrl = _project.PreviewUrl;
            Input.ProjectUrl = _project.ProjectUrl;
            Input.DeploymentNotes = _project.DeploymentNotes;
        }
    }

    private async Task SaveAsync()
    {
        if (_project is null) return;
        _submitting = true;
        _errorMessage = null;
        try
        {
            _project.Name = Input.Name;
            _project.Description = Input.Description;
            _project.Type = Input.Type;
            _project.Status = Input.Status;
            _project.PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim();
            _project.ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim();
            _project.DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim();

            await ProjectService.UpdateAsync(_project);
            NavigationManager.NavigateTo($"/Admin/Projects/{Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required] public string Name { get; set; } = "";
        [Required] public string Description { get; set; } = "";
        public ProjectType Type { get; set; } = ProjectType.Website;
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }
}
