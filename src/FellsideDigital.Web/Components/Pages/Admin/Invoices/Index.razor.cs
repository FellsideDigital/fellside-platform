using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Invoices;

public partial class Index : ComponentBase
{
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private IRecurringInvoiceService RecurringService { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;

    private bool _loading = true;
    private List<Invoice> _invoices = [];
    private List<RecurringInvoiceSchedule> _schedules = [];
    private readonly Dictionary<Guid, FileLinks> _fileLinks = [];

    // Filters
    private string _search = "";
    private InvoiceStatus? _statusFilter;

    // KPIs — across every client.
    private decimal _totalEarned => InvoiceEarnings.TotalEarned(_invoices, _schedules, DateTime.UtcNow);

    private decimal _outstandingTotal => _invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
        .Sum(i => i.Amount);

    private int _outstandingCount => _invoices.Count(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue);

    private decimal _overdueTotal => _invoices.Where(i => i.Status == InvoiceStatus.Overdue).Sum(i => i.Amount);
    private int _overdueCount => _invoices.Count(i => i.Status == InvoiceStatus.Overdue);

    private int _activeRetainers => _schedules.Count(s => s.IsActive);
    private decimal _monthlyRecurring => _schedules.Where(s => s.IsActive).Sum(s => s.Amount);

    private IReadOnlyList<Invoice> _filtered =>
        _invoices.Where(Matches).ToList();

    private bool Matches(Invoice i)
    {
        if (_statusFilter is { } status && i.Status != status) return false;
        if (string.IsNullOrWhiteSpace(_search)) return true;

        var q = _search.Trim();
        return Contains(i.Title, q)
            || Contains(i.Project?.Name, q)
            || Contains(ClientName(i.Project?.Client), q);
    }

    private static bool Contains(string? value, string q)
        => value is not null && value.Contains(q, StringComparison.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _invoices = await InvoiceService.GetAllAsync();
        _schedules = await RecurringService.GetAllSchedulesAsync();

        _fileLinks.Clear();
        foreach (var inv in _invoices.Where(i => i.FilePath is not null))
        {
            try
            {
                if (await InvoiceService.GetFileLinksAsync(inv.Id) is { } links)
                    _fileLinks[inv.Id] = links;
            }
            catch { /* non-fatal — the file links simply won't appear */ }
        }

        _loading = false;
    }

    private async Task ChangeStatusAsync(Guid invoiceId, ChangeEventArgs e)
    {
        if (!Enum.TryParse<InvoiceStatus>(e.Value?.ToString(), out var status)) return;
        try
        {
            await InvoiceService.UpdateStatusAsync(invoiceId, status);
            await LoadAsync();
            Toasts.Success($"Invoice marked as {status}.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the invoice status"));
        }
    }

    private void SetStatusFilter(InvoiceStatus? status) => _statusFilter = status;

    /// <summary>Display name for a client: "First Last · Company", falling back to email, then "Unassigned".</summary>
    internal static string ClientName(ApplicationUser? client)
    {
        if (client is null) return "Unassigned";
        var name = $"{client.FirstName} {client.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(name)) name = client.Email ?? "Client";
        return string.IsNullOrWhiteSpace(client.CompanyName) ? name : $"{name} · {client.CompanyName}";
    }

    /// <summary>Link to the client's dedicated invoices page, or empty when the project has no client yet.</summary>
    private static string ClientHref(Invoice i)
        => i.Project?.ClientId is { Length: > 0 } cid ? $"/Admin/Clients/{cid}/Invoices?from={i.ProjectId}" : "";

    private static string ClientHref(RecurringInvoiceSchedule s)
        => s.Project?.ClientId is { Length: > 0 } cid ? $"/Admin/Clients/{cid}/Invoices?from={s.ProjectId}" : "";
}
