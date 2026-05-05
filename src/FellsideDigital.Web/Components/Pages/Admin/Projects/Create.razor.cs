using System.ComponentModel.DataAnnotations;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Create : ComponentBase
{
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private InputModel Input { get; set; } = new();
    private List<ApplicationUser> _clients = [];
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-indigo-400 dark:focus:ring-orange-400 transition-shadow outline-none";

    protected override async Task OnInitializedAsync()
    {
        var allUsers = UserManager.Users.ToList();
        var adminIds = (await UserManager.GetUsersInRoleAsync("SiteAdmin"))
            .Select(u => u.Id)
            .ToHashSet();
        _clients = allUsers.Where(u => !adminIds.Contains(u.Id)).ToList();
    }

    private async Task CreateAsync()
    {
        _submitting = true;
        _errorMessage = null;
        try
        {
            var authState = await AuthState.GetAuthenticationStateAsync();
            var admin = await UserManager.GetUserAsync(authState.User);
            if (admin is null) { _errorMessage = "Could not identify admin user."; return; }

            var project = new ClientProject
            {
                ClientId = Input.ClientId,
                Name = Input.Name,
                Description = Input.Description,
                Type = Input.Type,
                Status = Input.Status,
                PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim(),
                ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim(),
                DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim()
            };

            await ProjectService.CreateAsync(project, admin.Id);
            NavigationManager.NavigateTo($"/Admin/Projects/{project.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create project: {ex.Message}";
        }
        finally
        {
            _submitting = false;
        }
    }

    private sealed class InputModel
    {
        [Required] public string ClientId { get; set; } = "";
        [Required] public string Name { get; set; } = "";
        [Required] public string Description { get; set; } = "";
        public ProjectType Type { get; set; } = ProjectType.Website;
        public ProjectStatus Status { get; set; } = ProjectStatus.Pending;
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }
}
