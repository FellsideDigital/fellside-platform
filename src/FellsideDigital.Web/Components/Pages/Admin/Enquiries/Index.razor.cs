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

    private int _unreadCount => _enquiries?.Count(e => !e.IsRead) ?? 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _enquiries = await EnquiryService.GetAllAsync();
    }

    private async Task OpenEnquiry(ContactEnquiry enquiry)
    {
        _selected = enquiry;

        // Viewing an enquiry marks it read, clearing the unread dot and header count.
        if (enquiry.IsRead) return;

        try
        {
            await EnquiryService.MarkAsReadAsync(enquiry.Id);
            enquiry.IsRead = true;
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the enquiry"));
        }
    }

    private void CloseDrawer()
    {
        _selected = null;
    }
}
