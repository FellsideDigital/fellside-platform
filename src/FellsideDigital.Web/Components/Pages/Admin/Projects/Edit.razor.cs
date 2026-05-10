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
    private List<PhaseEditorModel> _phases = [];
    private string? _errorMessage;
    private bool _submitting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    protected override async Task OnInitializedAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        if (_project is not null)
        {
            Input.Name = _project.Name;
            Input.Description = _project.Description;
            Input.Type = _project.Type;
            Input.Status = _project.Status;
            Input.TargetLaunchDate = _project.TargetLaunchDate;
            Input.PreviewUrl = _project.PreviewUrl;
            Input.ProjectUrl = _project.ProjectUrl;
            Input.DeploymentNotes = _project.DeploymentNotes;

            _phases = _project.PlanPhases
                .OrderBy(ph => ph.Order)
                .Select(ph => new PhaseEditorModel
                {
                    Title = ph.Title,
                    ShortLabel = ph.ShortLabel,
                    Status = ph.Status,
                    TargetCompletionDate = ph.TargetCompletionDate,
                    Notes = ph.Notes,
                    ImportantInformation = ph.ImportantInformation,
                    Dependencies = ph.Dependencies,
                    InternalNotes = ph.InternalNotes,
                    IsExpanded = false
                })
                .ToList();
        }
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
            _project.TargetLaunchDate = NormalizeToUtc(Input.TargetLaunchDate);
            _project.PreviewUrl = string.IsNullOrWhiteSpace(Input.PreviewUrl) ? null : Input.PreviewUrl.Trim();
            _project.ProjectUrl = string.IsNullOrWhiteSpace(Input.ProjectUrl) ? null : Input.ProjectUrl.Trim();
            _project.DeploymentNotes = string.IsNullOrWhiteSpace(Input.DeploymentNotes) ? null : Input.DeploymentNotes.Trim();

            await ProjectService.UpdateAsync(_project);

            var phases = _phases
                .Where(p => !string.IsNullOrWhiteSpace(p.Title) && !string.IsNullOrWhiteSpace(p.ShortLabel))
                .Select(p => new ProjectPlanPhase
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

            await ProjectService.SavePhasesAsync(_project.Id, phases);

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
