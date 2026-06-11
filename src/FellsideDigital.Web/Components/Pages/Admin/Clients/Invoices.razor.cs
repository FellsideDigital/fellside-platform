using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
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
    [Inject] private ILogger<Invoices> Logger { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;

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

    // Edit-invoice modal
    private Invoice? _editing;
    private string _editTitle = "";
    private decimal _editAmount;
    private string _editCurrency = "GBP";
    private DateTime? _editDueDate;
    private IBrowserFile? _editFile;
    private bool _notifyClient = true;
    private bool _saving;
    private string? _editError;

    private const string InputClass = FellsideDigital.UI.Components.Forms.FieldStyles.Input;

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
            _error = ErrorHandling.LogAndDescribe(Logger, ex, "uploading the invoice");
        }
        finally
        {
            _uploading = false;
        }
    }

    private void OpenEdit(Invoice inv)
    {
        _editing      = inv;
        _editTitle    = inv.Title;
        _editAmount   = inv.Amount;
        _editCurrency = inv.Currency;
        _editDueDate  = inv.DueAt?.ToLocalTime().Date;
        _editFile     = null;
        _notifyClient = true;
        _editError    = null;
    }

    private void CloseEdit() => _editing = null;

    private void OnEditFileSelected(InputFileChangeEventArgs e) => _editFile = e.File;

    private async Task SaveEditAsync()
    {
        if (_editing is null || string.IsNullOrWhiteSpace(_editTitle)) return;

        _saving = true;
        _editError = null;
        try
        {
            await InvoiceService.UpdateAsync(_editing.Id, _editTitle.Trim(), _editing.Description,
                _editAmount, _editCurrency, _editDueDate, _editFile, _notifyClient);
            _editing = null;
            await LoadAsync();
            Toasts.Success(_notifyClient ? "Invoice updated and the client was notified." : "Invoice updated.");
        }
        catch (InvalidOperationException ex)
        {
            _editError = ex.Message;
        }
        catch (Exception ex)
        {
            _editError = ErrorHandling.LogAndDescribe(Logger, ex, "updating the invoice");
        }
        finally
        {
            _saving = false;
        }
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

    private async Task DeleteInvoiceAsync(Guid invoiceId)
    {
        try
        {
            await InvoiceService.DeleteAsync(invoiceId);
            await LoadAsync();
            Toasts.Success("Invoice deleted.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the invoice"));
        }
    }
}
