using FellsideDigital.Domain.Enums;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Identity;

namespace FellsideDigital.Web.Components.Pages.Admin.Projects;

public partial class Detail : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IProjectService ProjectService { get; set; } = default!;
    [Inject] private IInvoiceService InvoiceService { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthState { get; set; } = default!;
    [Inject] private UserManager<ApplicationUser> UserManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private ClientProject? _project;
    private Dictionary<Guid, string> _downloadUrls = [];
    private string _updateMessage = "";
    private string _newStatus = "";
    private bool _postingUpdate;

    private string _invoiceTitle = "";
    private decimal _invoiceAmount;
    private string _invoiceCurrency = "GBP";
    private DateTime? _invoiceDueDate;
    private IBrowserFile? _selectedFile;
    private bool _uploadingInvoice;
    private string? _invoiceError;

    private bool _showDelete;
    private bool _deleting;

    private const string InputClass =
        "block w-full rounded-xl bg-gray-50 dark:bg-white/5 px-3.5 py-2.5 text-sm text-gray-900 dark:text-white " +
        "ring-1 ring-inset ring-gray-200 dark:ring-white/10 placeholder:text-gray-400 dark:placeholder:text-neutral-500 " +
        "focus:ring-2 focus:ring-inset focus:ring-accent transition-shadow outline-none";

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
                catch { /* non-fatal — download button simply won't appear */ }
            }
        }
    }

    private async Task PostUpdateAsync()
    {
        if (string.IsNullOrWhiteSpace(_updateMessage)) return;
        _postingUpdate = true;
        var authState = await AuthState.GetAuthenticationStateAsync();
        var admin = await UserManager.GetUserAsync(authState.User);
        ProjectStatus? status = Enum.TryParse<ProjectStatus>(_newStatus, out var parsed) ? parsed : null;
        await ProjectService.AddStatusUpdateAsync(Id, _updateMessage.Trim(), status, admin!.Id);
        _updateMessage = "";
        _newStatus = "";
        _postingUpdate = false;
        await LoadAsync();
    }

    private void OnFileSelected(InputFileChangeEventArgs e) => _selectedFile = e.File;

    private async Task UploadInvoiceAsync()
    {
        if (_selectedFile is null || string.IsNullOrWhiteSpace(_invoiceTitle)) return;
        _uploadingInvoice = true;
        _invoiceError = null;
        try
        {
            await InvoiceService.UploadAsync(Id, _invoiceTitle.Trim(), null, _invoiceAmount,
                _invoiceCurrency, _invoiceDueDate, _selectedFile);
            _invoiceTitle = "";
            _invoiceAmount = 0;
            _invoiceDueDate = null;
            _selectedFile = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _invoiceError = ex.Message;
        }
        finally
        {
            _uploadingInvoice = false;
        }
    }

    private async Task ChangeInvoiceStatusAsync(Guid invoiceId, ChangeEventArgs e)
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

    private async Task ConfirmDeleteAsync()
    {
        _deleting = true;
        await ProjectService.DeleteAsync(Id);
        NavigationManager.NavigateTo("/Admin/Projects");
    }
}
