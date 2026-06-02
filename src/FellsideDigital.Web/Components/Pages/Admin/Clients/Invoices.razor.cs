using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Clients;

public partial class Invoices : ComponentBase
{
    [Parameter] public string ClientId { get; set; } = "";
    [SupplyParameterFromQuery] public Guid? From { get; set; }

    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;

    private bool _loading = true;
    private ApplicationUser? _client;
    private List<ClientProject> _projects = [];
    private List<Invoice> _invoices = [];
    private readonly Dictionary<Guid, string> _downloadUrls = [];

    // Add-invoice form
    private string _selectedProjectId = "";
    private string _title = "";
    private decimal _amount;
    private string _currency = "GBP";
    private DateTime? _dueDate;
    private IBrowserFile? _selectedFile;
    private bool _uploading;
    private string? _error;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

    private string _backHref => From.HasValue ? $"/Admin/Projects/{From}" : "/Admin/Projects";

    private string _clientLine
    {
        get
        {
            if (_client is null) return "";
            var name = $"{_client.FirstName} {_client.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(name)) name = _client.Email ?? "Client";
            return string.IsNullOrWhiteSpace(_client.CompanyName) ? name : $"{name} · {_client.CompanyName}";
        }
    }

    private decimal _outstandingTotal => _invoices
        .Where(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue)
        .Sum(i => i.Amount);

    private decimal _paidTotal => _invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.Amount);

    private string _outstandingSub
    {
        get
        {
            var count = _invoices.Count(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue);
            return count == 0 ? "All settled" : $"{count} invoice{(count == 1 ? "" : "s")} due";
        }
    }

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _client = await UserManager.FindByIdAsync(ClientId);
        _projects = await ProjectService.GetForClientAsync(ClientId);
        _invoices = await InvoiceService.GetForClientAsync(ClientId);

        _downloadUrls.Clear();
        foreach (var inv in _invoices.Where(i => i.FilePath is not null))
        {
            try { _downloadUrls[inv.Id] = await InvoiceService.GetDownloadUrlAsync(inv.Id) ?? ""; }
            catch { /* non-fatal — download link simply won't appear */ }
        }

        _loading = false;
    }

    private void OnFileSelected(InputFileChangeEventArgs e) => _selectedFile = e.File;

    private async Task AddInvoiceAsync()
    {
        if (_selectedFile is null || string.IsNullOrWhiteSpace(_title)) return;
        if (!Guid.TryParse(_selectedProjectId, out var projectId)) { _error = "Select a project."; return; }

        _uploading = true;
        _error = null;
        try
        {
            await InvoiceService.UploadAsync(projectId, _title.Trim(), null, _amount,
                _currency, _dueDate, _selectedFile);
            _title = "";
            _amount = 0;
            _dueDate = null;
            _selectedFile = null;
            _selectedProjectId = "";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _uploading = false;
        }
    }

    private async Task ChangeStatusAsync(Guid invoiceId, ChangeEventArgs e)
    {
        if (Enum.TryParse<InvoiceStatus>(e.Value?.ToString(), out var status))
            await InvoiceService.UpdateStatusAsync(invoiceId, status);
        await LoadAsync();
    }

    private async Task DeleteInvoiceAsync(Guid invoiceId)
    {
        await InvoiceService.DeleteAsync(invoiceId);
        await LoadAsync();
    }
}
