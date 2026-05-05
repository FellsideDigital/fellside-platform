using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace FellsideDigital.Web.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

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
