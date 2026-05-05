using FellsideDigital.Web.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace FellsideDigital.Web.Components.Pages.Admin.Enquiries;

public partial class Index : ComponentBase
{
    [Inject] private FellsideDigitalDbContext Db { get; set; } = default!;

    private List<ContactEnquiry>? _enquiries;
    private ContactEnquiry? _selected;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _enquiries = await Db.ContactEnquiries
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync();
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

        var entity = await Db.ContactEnquiries.FindAsync(_selected.Id);
        if (entity is not null)
        {
            entity.IsRead = true;
            await Db.SaveChangesAsync();
        }

        _selected.IsRead = true;
        StateHasChanged();
    }
}
