using FellsideDigital.Domain.Enums;
using FellsideDigital.UI.Components.Feedback;
using FellsideDigital.Web.Data;
using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;

namespace FellsideDigital.Web.Components.Pages.Admin.Testimonials;

public partial class Index : ComponentBase
{
    [Inject] private ITestimonialService Testimonials { get; set; } = default!;
    [Inject] private ToastService Toasts { get; set; } = default!;
    [Inject] private ILogger<Index> Logger { get; set; } = default!;

    private List<ClientTestimonial>? _testimonials;

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync() => _testimonials = await Testimonials.GetAllAsync();

    private async Task ApproveAsync(Guid id) => await SetStatusAsync(id, TestimonialStatus.Approved, "Testimonial approved and published.");

    private async Task RejectAsync(Guid id) => await SetStatusAsync(id, TestimonialStatus.Rejected, "Testimonial rejected.");

    private async Task SetStatusAsync(Guid id, TestimonialStatus status, string successMessage)
    {
        try
        {
            await Testimonials.SetStatusAsync(id, status);
            await ReloadAsync();
            Toasts.Success(successMessage);
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "updating the testimonial"));
        }
    }

    private async Task DeleteAsync(Guid id)
    {
        try
        {
            await Testimonials.DeleteAsync(id);
            await ReloadAsync();
            Toasts.Success("Testimonial deleted.");
        }
        catch (Exception ex)
        {
            Toasts.Error(ErrorHandling.LogAndDescribe(Logger, ex, "deleting the testimonial"));
        }
    }
}
