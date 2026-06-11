using FellsideDigital.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FellsideDigital.Web.Components.Pages.Marketing;

public partial class Home : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ITestimonialService Testimonials { get; set; } = default!;
    [Inject] private ILogger<Home> Logger { get; set; } = default!;

    /// <summary>A testimonial to render on the public page (quote, attribution, stars).</summary>
    public sealed record TestimonialView(string Quote, string Name, string Role, int Rating);

    private IReadOnlyList<TestimonialView> _testimonials = [];

    /// <summary>
    /// Loads approved testimonials. If there are none (or the lookup fails) the list stays
    /// empty and the section renders an empty state instead.
    /// </summary>
    private async Task LoadTestimonialsAsync()
    {
        try
        {
            _testimonials = (await Testimonials.GetApprovedAsync())
                .Select(t => new TestimonialView(t.Quote, t.AuthorName, t.AuthorRole, t.Rating))
                .ToList();
        }
        catch (Exception ex)
        {
            ErrorHandling.LogAndDescribe(Logger, ex, "loading testimonials for the home page");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await JS.InvokeVoidAsync("fellsideTheme.init");
        await JS.InvokeVoidAsync("fellsideScroll.init");

        await JS.InvokeVoidAsync("fellsideAnime.fadeUp", "#site-nav",
            new { distance = -12, duration = 500, startDelay = 0 });

        await JS.InvokeVoidAsync("fellsideAnime.flipInWordsOnScroll", "#services-heading",
            new { duration = 700, stagger = 120 });
        await JS.InvokeVoidAsync("fellsideAnime.elasticPopOnScroll", "#services-grid > div",
            new { stagger = 100, startDelay = 50 });

        await JS.InvokeVoidAsync("fellsideAnime.fadeUp", "#about-heading",
            new { distance = 16, duration = 600 });
        await JS.InvokeVoidAsync("fellsideAnime.fadeUp", "#about-body1",
            new { distance = 20, duration = 700, startDelay = 150 });
        await JS.InvokeVoidAsync("fellsideAnime.fadeUp", "#about-body2",
            new { distance = 20, duration = 700, startDelay = 280 });
        await JS.InvokeVoidAsync("fellsideAnime.zoomInOnScroll", "#about-image",
            new { from = 0.92, duration = 700 });

        await JS.InvokeVoidAsync("fellsideAnime.flipInWordsOnScroll", "#testimonials-heading",
            new { duration = 700, stagger = 120 });
        await JS.InvokeVoidAsync("fellsideAnime.zoomInOnScroll", "#testimonials-grid > div",
            new { from = 0.92, duration = 700, stagger = 120 });

        await JS.InvokeVoidAsync("fellsideAnime.flipInWordsOnScroll", "#faqs-heading",
            new { duration = 700, stagger = 120 });
        await JS.InvokeVoidAsync("fellsideAnime.fadeUp", "#faqs-list > div",
            new { distance = 16, duration = 500, stagger = 60 });

        await JS.InvokeVoidAsync("fellsideAnime.blurClear", "#cta-heading",
            new { duration = 900, stagger = 60, distance = 10 });
    }
}
