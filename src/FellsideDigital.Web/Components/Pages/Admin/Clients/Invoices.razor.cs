using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.UI.Components.Navigation;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Models;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FellsideDigital.Web.Components.Pages.Admin.Clients;

public partial class Invoices : ComponentBase
{
    [Parameter] public string ClientId { get; set; } = "";
    [SupplyParameterFromQuery] public Guid? From { get; set; }

    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private IRecurringInvoiceService RecurringService { get; set; } = default!;
    [Inject] private IOptions<BillingSettings> BillingOptions { get; set; } = default!;
    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private ILogger<Invoices> Logger { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;

    private bool _loading = true;
    private ApplicationUser? _client;
    private List<ClientProject> _projects = [];
    private List<Invoice> _invoices = [];
    private readonly Dictionary<Guid, FileLinks> _downloadUrls = [];

    // Add-invoice form
    private string _selectedProjectId = "";
    private string _title = "";
    private decimal _amount;
    private string _currency = "GBP";
    private DateTime? _dueDate;
    private IBrowserFile? _selectedFile;
    private bool _uploading;
    private string? _error;

    // Recurring schedules
    private List<RecurringInvoiceSchedule> _schedules = [];
    private string _recProjectId = "";
    private string _recTitle = "";
    private decimal _recAmount;
    private string _recCurrency = "GBP";
    private int _recPaymentDay = 1;
    private DateTime? _recFirstPaymentDate = DateTime.Today;
    private bool _recSaving;
    private string? _recError;
    private int _paymentDay => BillingOptions.Value.PaymentDayOfMonth;

    // Edit-schedule modal
    private RecurringInvoiceSchedule? _editingSchedule;
    private string _editScheduleTitle = "";
    private decimal _editScheduleAmount;
    private string _editScheduleCurrency = "GBP";
    private int _editSchedulePaymentDay = 1;
    private DateTime? _editScheduleFirstPaymentDate = DateTime.Today;
    private bool _savingSchedule;
    private string? _editScheduleError;

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

    /// <summary>Breadcrumb trail: Projects › {project, when arrived from one} › Invoices.</summary>
    private IReadOnlyList<BreadcrumbItem> _crumbs
    {
        get
        {
            var crumbs = new List<BreadcrumbItem> { new("Projects", "/Admin/Projects") };
            if (From is { } fromId && _projects.FirstOrDefault(p => p.Id == fromId) is { } project)
            {
                crumbs.Add(new BreadcrumbItem(project.Name, $"/Admin/Projects/{fromId}"));
            }
            crumbs.Add(new BreadcrumbItem("Invoices"));
            return crumbs;
        }
    }

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

    /// <summary>Real revenue: paid one-off invoices plus estimated retainer collections. See <see cref="InvoiceEarnings"/>.</summary>
    private decimal _totalEarned => InvoiceEarnings.TotalEarned(_invoices, _schedules, DateTime.UtcNow);

    private string _outstandingSub
    {
        get
        {
            var count = _invoices.Count(i => i.Status is InvoiceStatus.Sent or InvoiceStatus.Overdue);
            return count == 0 ? "All settled" : $"{count} invoice{(count == 1 ? "" : "s")} due";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        _recPaymentDay = _paymentDay; // default new schedules to the global payment day
        _recFirstPaymentDate = DateTime.Today;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _client = await UserManager.FindByIdAsync(ClientId);
        _projects = await ProjectService.GetForClientAsync(ClientId);
        _invoices = await InvoiceService.GetForClientAsync(ClientId);
        _schedules = await RecurringService.GetForClientAsync(ClientId);

        _downloadUrls.Clear();
        foreach (var inv in _invoices.Where(i => i.FilePath is not null))
        {
            try
            {
                if (await InvoiceService.GetFileLinksAsync(inv.Id) is { } links)
                    _downloadUrls[inv.Id] = links;
            }
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

    private async Task AddScheduleAsync()
    {
        if (string.IsNullOrWhiteSpace(_recTitle)) return;
        if (!Guid.TryParse(_recProjectId, out var projectId)) { _recError = "Select a project."; return; }

        _recSaving = true;
        _recError = null;
        try
        {
            await RecurringService.CreateAsync(projectId, _recTitle.Trim(), null, _recAmount, _recCurrency,
                paymentDay: _recPaymentDay, firstPaymentDate: _recFirstPaymentDate);
            _recProjectId = "";
            _recTitle = "";
            _recAmount = 0;
            _recPaymentDay = _paymentDay;
            _recFirstPaymentDate = DateTime.Today;
            await LoadAsync();
            Toasts.Success("Recurring invoice scheduled.");
        }
        catch (InvalidOperationException ex)
        {
            _recError = ex.Message;
        }
        catch (Exception ex)
        {
            _recError = ErrorHandling.LogAndDescribe(Logger, ex, "creating the recurring invoice");
        }
        finally
        {
            _recSaving = false;
        }
    }

    private void OpenScheduleEdit(RecurringInvoiceSchedule schedule)
    {
        _editingSchedule             = schedule;
        _editScheduleTitle           = schedule.Title;
        _editScheduleAmount          = schedule.Amount;
        _editScheduleCurrency        = schedule.Currency;
        _editSchedulePaymentDay      = schedule.PaymentDayOfMonth;
        _editScheduleFirstPaymentDate = schedule.FirstPaymentDate;
        _editScheduleError           = null;
    }

    private void CloseScheduleEdit() => _editingSchedule = null;

    private async Task SaveScheduleEditAsync()
    {
        if (_editingSchedule is null || string.IsNullOrWhiteSpace(_editScheduleTitle)) return;

        _savingSchedule = true;
        _editScheduleError = null;
        try
        {
            await RecurringService.UpdateAsync(_editingSchedule.Id, _editScheduleTitle.Trim(),
                _editingSchedule.Description, _editScheduleAmount, _editScheduleCurrency, _editSchedulePaymentDay,
                _editScheduleFirstPaymentDate ?? DateTime.Today);
            _editingSchedule = null;
            await LoadAsync();
            Toasts.Success("Recurring invoice updated.");
        }
        catch (InvalidOperationException ex)
        {
            _editScheduleError = ex.Message;
        }
        catch (Exception ex)
        {
            _editScheduleError = ErrorHandling.LogAndDescribe(Logger, ex, "updating the recurring invoice");
        }
        finally
        {
            _savingSchedule = false;
        }
    }

    private async Task ToggleScheduleAsync(RecurringInvoiceSchedule schedule)
    {
        try
        {
            await RecurringService.SetActiveAsync(schedule.Id, !schedule.IsActive);
            await LoadAsync();
            Toasts.Success(schedule.IsActive ? "Recurring invoice paused." : "Recurring invoice resumed.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the recurring invoice"));
        }
    }

    private async Task DeleteScheduleAsync(Guid scheduleId)
    {
        try
        {
            await RecurringService.DeleteAsync(scheduleId);
            await LoadAsync();
            Toasts.Success("Recurring invoice deleted. Invoices already issued are kept.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the recurring invoice"));
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
