using FellsideDigital.Domain.Enums;
using FellsideDigital.Domain.Extensions;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Detail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private Dictionary<Guid, string> _downloadUrls = [];

    private bool _showDelete;
    private bool _deleting;

    // ── Snapshot computeds (project + client overview) ──

    private string TypeLabel => _project?.Type switch
    {
        ProjectType.Website => "Marketing website",
        ProjectType.Automation => "Automation project",
        _ => _project?.Type.ToString() ?? ""
    };

    private int ProgressPct
    {
        get
        {
            if (_project is null) return 0;
            var phases = _project.PlanPhases;
            if (phases.Count == 0) return _project.Progress;
            var done = phases.Count(p => p.Status == PhaseStatus.Completed);
            return (int)Math.Round((double)done / phases.Count * 100);
        }
    }

    private int CompletedPhaseCount => _project?.PlanPhases.Count(p => p.Status == PhaseStatus.Completed) ?? 0;

    // First not-yet-complete phase by order, falling back to the last phase.
    private ProjectPlanPhase? CurrentPhase =>
        _project?.PlanPhases.OrderBy(p => p.Order).FirstOrDefault(p => p.Status != PhaseStatus.Completed)
        ?? _project?.PlanPhases.OrderBy(p => p.Order).LastOrDefault();

    private string CurrentPhaseShort =>
        CurrentPhase?.ShortLabel is { Length: > 0 } s ? s
        : CurrentPhase?.Title is { Length: > 0 } t ? t
        : "Not started";

    private string CurrentPhaseSub => CurrentPhase?.Status.DisplayName() ?? "No phases yet";

    private string DaysToLaunchStr
    {
        get
        {
            if (_project?.TargetLaunchDate is null) return "—";
            var days = (int)(_project.TargetLaunchDate.Value.Date - DateTime.Today).TotalDays;
            return days >= 0 ? days.ToString() : "0";
        }
    }

    private string TargetLaunchSub => _project?.TargetLaunchDate is not null
        ? "Target " + _project.TargetLaunchDate.Value.ToLocalTime().ToString("d MMM yyyy")
        : "No target set";

    private decimal OutstandingTotal => _project?.Invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
        .Sum(i => i.Amount) ?? 0m;

    private string OutstandingStr => $"£{OutstandingTotal:N0}";

    private string OutstandingSub
    {
        get
        {
            var count = _project?.Invoices.Count(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue) ?? 0;
            return count == 0 ? "All settled" : $"{count} invoice{(count == 1 ? "" : "s")} due";
        }
    }

    private string ClientName
    {
        get
        {
            var c = _project?.Client;
            if (c is null) return "Unknown client";
            var full = $"{c.FirstName} {c.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(full)) return full;
            if (!string.IsNullOrWhiteSpace(c.CompanyName)) return c.CompanyName!;
            return c.Email ?? "Unknown client";
        }
    }

    private string ClientInitials
    {
        get
        {
            var c = _project?.Client;
            if (c is null) return "?";
            var f = c.FirstName?.Trim();
            var l = c.LastName?.Trim();
            if (!string.IsNullOrEmpty(f) && !string.IsNullOrEmpty(l))
                return $"{char.ToUpper(f[0])}{char.ToUpper(l[0])}";
            var basis = !string.IsNullOrWhiteSpace(c.CompanyName) ? c.CompanyName : c.Email;
            return string.IsNullOrWhiteSpace(basis) ? "?" : char.ToUpper(basis!.Trim()[0]).ToString();
        }
    }

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _project = await ProjectService.GetByIdAsync(Id);
        _downloadUrls = [];

        if (_project?.Invoices is not null)
        {
            foreach (var inv in _project.Invoices.Where(i => i.FilePath is not null))
            {
                try { _downloadUrls[inv.Id] = await InvoiceService.GetDownloadUrlAsync(inv.Id) ?? ""; }
                catch { /* non-fatal — download link simply won't appear */ }
            }
        }
    }

    private async Task ConfirmDeleteAsync()
    {
        _deleting = true;
        await ProjectService.DeleteAsync(Id);
        NavigationManager.NavigateTo("/Admin/Projects");
    }
}
