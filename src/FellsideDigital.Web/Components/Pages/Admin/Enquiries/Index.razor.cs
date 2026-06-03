using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Enquiries;

public partial class Index : ComponentBase
{
    [Inject] private IEnquiryService EnquiryService { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private List<ContactEnquiry>? _enquiries;
    private ContactEnquiry? _selected;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _enquiries = await EnquiryService.GetAllAsync();
    }

    private void OpenEnquiry(ContactEnquiry enquiry)
    {
        _selected = enquiry;
    }

    private void CloseDrawer()
    {
        _selected = null;
    }

    private async Task MarkAsRead()
    {
        if (_selected is null) return;

        try
        {
            await EnquiryService.MarkAsReadAsync(_selected.Id);
            _selected.IsRead = true;
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the enquiry"));
        }
        StateHasChanged();
    }
}
