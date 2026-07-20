using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Portal;

public partial class Invoices : ComponentBase
{
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private IRecurringInvoiceService RecurringService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private PortalPreviewState PreviewState { get; set; } = default!;

    private List<Invoice>? _invoices;
    private List<RecurringInvoiceSchedule> _schedules = [];
    private Dictionary<Guid, FileLinks> _fileLinks = [];

    /// <summary>The client's combined recurring bill each month (active schedules only).</summary>
    private decimal _monthlyTotal => _schedules.Sum(s => s.Amount);

    /// <summary>Estimated total collected from active retainers to date (months elapsed × amount).</summary>
    private decimal _retainerCollected =>
        _schedules.Sum(s => RecurringService.CollectedToDate(s, DateTime.UtcNow));

    /// <summary>
    /// Total paid to date: retainer payments collected by Direct Debit, plus one-off invoices
    /// marked paid. Schedule-generated invoices are excluded here — they're counted via the
    /// retainer estimate above, so they aren't double-counted.
    /// </summary>
    private decimal _paidTotal =>
        _retainerCollected
        + (_invoices?.Where(i => i.Status == InvoiceStatus.Paid && i.ScheduleId is null).Sum(i => i.Amount) ?? 0m);

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthState.GetAuthenticationStateAsync();
        var user = await UserManager.GetUserAsync(authState.User);
        if (user is null) return;
        var clientId = PreviewState.ResolveClientId(user.Id, authState.User.IsInRole("SiteAdmin"));
        _invoices = await InvoiceService.GetForClientAsync(clientId);
        _schedules = (await RecurringService.GetForClientAsync(clientId))
            .Where(s => s.IsActive)
            .ToList();

        // Presign view + download URLs for every invoice that has a file. The raw FilePath is
        // an S3 object key, not a browsable URL, so it must go through the storage service.
        _fileLinks = [];
        foreach (var inv in _invoices.Where(i => i.FilePath is not null))
        {
            try
            {
                if (await InvoiceService.GetFileLinksAsync(inv.Id) is { } links)
                    _fileLinks[inv.Id] = links;
            }
            catch { /* non-fatal — the file actions simply won't render for this row */ }
        }
    }
}
