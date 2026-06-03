using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Enquiries;

public partial class Index : ComponentBase
{
    [Inject] private IEnquiryService EnquiryService { get; set; } = default!;

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

        await EnquiryService.MarkAsReadAsync(_selected.Id);
        _selected.IsRead = true;
        StateHasChanged();
    }
}
