using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Edit : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IHeroProjectService HeroProjectService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private InputModel Input { get; set; } = new();
    private List<PhaseEditorModel> _phases = [];
    private string? _errorMessage;
    private bool _submitting;

    private string _activeTab = "details";

    private string TabClass(string tab) =>
        tab == _activeTab
            ? "inline-flex items-center px-4 py-2.5 text-sm font-semibold text-accent border-b-2 border-accent -mb-px"
            : "inline-flex items-center px-4 py-2.5 text-sm font-medium text-gray-500 dark:text-neutral-400 border-b-2 border-transparent hover:text-gray-900 dark:hover:text-white transition-colors";

    // A validation failure can occur on a field that lives on the Details tab while
    // the user is looking at Plan — surface it by snapping back to Details.
    private void OnInvalidEdit() => _activeTab = "details";

    // Hero showcase state
    private HeroInputModel _heroInput = new();
    private List<MetricEditorModel> _heroMetrics = [];
    private List<PipelineStepEditorModel> _heroPipelineSteps = [];
    private List<IntegrationEditorModel> _heroIntegrations = [];
    private bool _savingHero;
    private string? _heroError;
    private bool _heroSaved;

    // Screenshot upload state
    private bool _uploadingScreenshot;
    private string? _screenshotPreviewUrl;
    private string? _screenshotError;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

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

            _heroInput = new HeroInputModel
            {
                IsHeroProject    = _project.IsHeroProject,
                HeroDisplayOrder = _project.HeroDisplayOrder,
                HeroTagline      = _project.HeroTagline,
                HeroShowcaseUrl  = _project.HeroShowcaseUrl,
                ScreenshotPath   = _project.ScreenshotPath
            };

            _heroMetrics = _project.Metrics
                .OrderBy(m => m.DisplayOrder)
                .Select(m => new MetricEditorModel { Value = m.Value, Label = m.Label, Style = m.Style })
                .ToList();

            _heroPipelineSteps = _project.PipelineSteps
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new PipelineStepEditorModel { Label = s.Label, StepType = s.StepType })
                .ToList();

            _heroIntegrations = _project.Integrations
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new IntegrationEditorModel { Name = i.Name })
                .ToList();

            _screenshotPreviewUrl = await HeroProjectService.ResolveScreenshotUrlAsync(_project.ScreenshotPath);
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

            var authState = await AuthState.GetAuthenticationStateAsync();
            var actorId = authState.User.FindFirstValue(ClaimTypes.NameIdentifier);

            await ProjectService.UpdateAsync(_project, actorId);

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

            await ProjectService.SavePhasesAsync(_project.Id, phases, actorId);

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

    // Hero showcase methods
    private void AddMetric()
    {
        if (_heroMetrics.Count < 3) _heroMetrics.Add(new MetricEditorModel());
    }

    private void RemoveMetric(int index)
    {
        if (index >= 0 && index < _heroMetrics.Count) _heroMetrics.RemoveAt(index);
    }

    private void AddPipelineStep()
    {
        if (_heroPipelineSteps.Count < 5) _heroPipelineSteps.Add(new PipelineStepEditorModel());
    }

    private void RemovePipelineStep(int index)
    {
        if (index >= 0 && index < _heroPipelineSteps.Count) _heroPipelineSteps.RemoveAt(index);
    }

    private void AddIntegration() => _heroIntegrations.Add(new IntegrationEditorModel());

    private void RemoveIntegration(int index)
    {
        if (index >= 0 && index < _heroIntegrations.Count) _heroIntegrations.RemoveAt(index);
    }

    private async Task OnScreenshotSelectedAsync(InputFileChangeEventArgs e)
    {
        if (_project is null) return;
        _screenshotError = null;
        _uploadingScreenshot = true;
        try
        {
            var key = await HeroProjectService.UploadScreenshotAsync(_project.Id, e.File);
            _heroInput.ScreenshotPath = key;
            _screenshotPreviewUrl = await HeroProjectService.ResolveScreenshotUrlAsync(key);
        }
        catch (Exception ex)
        {
            _screenshotError = ex.Message;
        }
        finally
        {
            _uploadingScreenshot = false;
        }
    }

    private async Task RemoveScreenshotAsync()
    {
        if (_project is null) return;
        _screenshotError = null;
        try
        {
            await HeroProjectService.RemoveScreenshotAsync(_project.Id);
            _heroInput.ScreenshotPath = null;
            _screenshotPreviewUrl = null;
        }
        catch (Exception ex)
        {
            _screenshotError = ex.Message;
        }
    }

    private async Task SaveHeroAsync()
    {
        if (_project is null) return;
        _savingHero = true;
        _heroError = null;
        _heroSaved = false;
        try
        {
            await HeroProjectService.SaveHeroSettingsAsync(
                _project.Id,
                _heroInput.IsHeroProject,
                _heroInput.HeroDisplayOrder,
                _heroInput.HeroTagline,
                _heroInput.HeroShowcaseUrl,
                _heroInput.ScreenshotPath);

            await HeroProjectService.SaveMetricsAsync(_project.Id,
                _heroMetrics.Select(m => new FellsideDigital.Web.Data.ProjectMetric
                {
                    Value = m.Value,
                    Label = m.Label,
                    Style = m.Style
                }).ToList());

            await HeroProjectService.SavePipelineStepsAsync(_project.Id,
                _heroPipelineSteps.Select(s => new FellsideDigital.Web.Data.ProjectPipelineStep
                {
                    Label = s.Label,
                    StepType = s.StepType
                }).ToList());

            await HeroProjectService.SaveIntegrationsAsync(_project.Id,
                _heroIntegrations.Select(i => new FellsideDigital.Web.Data.ProjectIntegration
                {
                    Name = i.Name
                }).ToList());

            _heroSaved = true;
        }
        catch (Exception ex)
        {
            _heroError = $"Failed to save: {ex.Message}";
        }
        finally
        {
            _savingHero = false;
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

    private sealed class HeroInputModel
    {
        public bool IsHeroProject { get; set; }
        public int HeroDisplayOrder { get; set; }
        public string? HeroTagline { get; set; }
        public string? HeroShowcaseUrl { get; set; }
        public string? ScreenshotPath { get; set; }
    }

    private sealed class MetricEditorModel
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public MetricStyle Style { get; set; } = MetricStyle.Neutral;
    }

    private sealed class PipelineStepEditorModel
    {
        public string Label { get; set; } = "";
        public PipelineStepType StepType { get; set; } = PipelineStepType.Process;
    }

    private sealed class IntegrationEditorModel
    {
        public string Name { get; set; } = "";
    }
}
