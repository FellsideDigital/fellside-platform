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
    [Inject] private ILogger<Create> Logger { get; set; } = default!;

    private InputModel Input { get; set; } = new();
    private List<ApplicationUser> _clients = [];
    private List<PhaseEditorModel> _phases = [];
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

    protected override async Task OnInitializedAsync()
    {
        var allUsers = UserManager.Users.ToList();
        var adminIds = (await UserManager.GetUsersInRoleAsync("SiteAdmin"))
            .Select(u => u.Id)
            .ToHashSet();
        _clients = allUsers.Where(u => !adminIds.Contains(u.Id)).ToList();
    }

    private static DateTime? NormalizeToUtc(DateTime? value)
        => value switch
        {
            null => null,
            { Kind: DateTimeKind.Utc } dt => dt,
            { Kind: DateTimeKind.Local } dt => dt.ToUniversalTime(),
            { Kind: DateTimeKind.Unspecified } dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
            var dt => dt
        };

    private void AddPhase()
    {
        if (_phases.Count >= 5) return;
        _phases.Add(new PhaseEditorModel { IsExpanded = true });
    }

    private void RemovePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases.RemoveAt(index);
    }

    private void MovePhaseUp(int index)
    {
        if (index <= 0 || index >= _phases.Count) return;
        (_phases[index - 1], _phases[index]) = (_phases[index], _phases[index - 1]);
    }

    private void MovePhaseDown(int index)
    {
        if (index < 0 || index >= _phases.Count - 1) return;
        (_phases[index], _phases[index + 1]) = (_phases[index + 1], _phases[index]);
    }

    private void TogglePhase(int index)
    {
        if (index < 0 || index >= _phases.Count) return;
        _phases[index].IsExpanded = !_phases[index].IsExpanded;
    }

    private void OnPhaseTargetDateChange(int index, string? value)
    {
        _phases[index].TargetCompletionDate = string.IsNullOrEmpty(value)
            ? null
            : DateTime.TryParse(value, out var d) ? NormalizeToUtc(d) : null;
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
                TargetLaunchDate = NormalizeToUtc(Input.TargetLaunchDate),
                PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim(),
                ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim(),
                DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim()
            };

            await ProjectService.CreateAsync(project, admin.Id);

            var validPhases = _phases
                .Where(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.ShortLabel))
                .ToList();

            if (validPhases.Count > 0)
            {
                var phases = validPhases.Select(p => new ProjectPlanPhase
                {
                    Title = p.Title.Trim(),
                    ShortLabel = p.ShortLabel.Trim(),
                    Status = p.Status,
                    TargetCompletionDate = NormalizeToUtc(p.TargetCompletionDate),
                    Notes = string.IsNullOrWhiteSpace(p.Notes) ? null : p.Notes.Trim(),
                    ImportantInformation = string.IsNullOrWhiteSpace(p.ImportantInformation) ? null : p.ImportantInformation.Trim(),
                    Dependencies = string.IsNullOrWhiteSpace(p.Dependencies) ? null : p.Dependencies.Trim(),
                    InternalNotes = string.IsNullOrWhiteSpace(p.InternalNotes) ? null : p.InternalNotes.Trim()
                }).ToList();

                await ProjectService.SavePhasesAsync(project.Id, phases);
            }

            NavigationManager.NavigateTo($"/Admin/Projects/{project.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = ErrorHandling.LogAndDescribe(Logger, ex, "creating the project");
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
        public DateTime? TargetLaunchDate { get; set; }
        public string? PreviewUrl { get; set; }
        public string? ProjectUrl { get; set; }
        public string? DeploymentNotes { get; set; }
    }

    private sealed class PhaseEditorModel
    {
        public string Title { get; set; } = "";
        public string ShortLabel { get; set; } = "";
        public PhaseStatus Status { get; set; } = PhaseStatus.NotStarted;
        public DateTime? TargetCompletionDate { get; set; }
        public string? Notes { get; set; }
        public string? ImportantInformation { get; set; }
        public string? Dependencies { get; set; }
        public string? InternalNotes { get; set; }
        public bool IsExpanded { get; set; } = true;
    }
}
