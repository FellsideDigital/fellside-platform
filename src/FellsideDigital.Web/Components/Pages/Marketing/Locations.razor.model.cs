namespace FellsideDigital.Web.Components.Pages.Marketing;

// ─── Location landing-page data ───────────────────────────────────────────────
// One record per town. Content is deliberately unique per town (local context,
// nearby areas, sectors, town-specific FAQs) to provide real value per page and
// avoid thin/doorway-content penalties. See programmatic-seo conventions.

public record LocationFaq(string Question, string Answer);

public record Location(
    string Slug,            // URL segment, e.g. "keswick"
    string Town,            // Display name, e.g. "Keswick"
    string PostcodeArea,    // e.g. "CA12"
    double Latitude,
    double Longitude,
    string MetaTitle,
    string MetaDescription,
    string HeroKicker,
    string Lead,            // One-paragraph local lead (definition-style, AI-extractable)
    string LocalContext,    // Second paragraph: genuine local detail
    string[] NearbyAreas,   // Surrounding places also served
    string[] LocalSectors,  // Typical local business types
    string TravelNote,      // Trust signal re: distance from Keswick base
    LocationFaq[] Faqs);

public static class LocationData
{
    // Keswick base coordinates, used for the "based in / serving" relationship.
    public const double BaseLatitude = 54.6013;
    public const double BaseLongitude = -3.1347;

    public static readonly IReadOnlyList<Location> All = new[]
    {
        new Location(
            Slug: "keswick",
            Town: "Keswick",
            PostcodeArea: "CA12",
            Latitude: 54.6013,
            Longitude: -3.1347,
            MetaTitle: "Website Design in Keswick | Web Developer | Fellside Digital",
            MetaDescription: "Bespoke website design and web development in Keswick, Cumbria. A local, founder-led studio building fast, hand-coded sites for Keswick businesses. Fixed-price quotes.",
            HeroKicker: "Website Design in Keswick",
            Lead: "Fellside Digital is a website design and web development studio based in Keswick, Cumbria. We build bespoke, hand-coded websites — no templates, no page builders — for businesses in and around Keswick, with fixed prices agreed before any work starts.",
            LocalContext: "Keswick is our home. We know the make-up of the local economy here: independent retailers on Main Street, guest houses and self-catering lets, outdoor and adventure operators, cafés and hospitality. A website for a Keswick business has to load fast on patchy rural mobile signal, rank for visitors searching before they arrive, and look the part against a town full of strong independent brands. That is exactly what we build for.",
            NearbyAreas: new[] { "Portinscale", "Braithwaite", "Threlkeld", "Borrowdale", "Bassenthwaite", "Keswick CA12" },
            LocalSectors: new[] { "Holiday lets & guest houses", "Outdoor & adventure", "Independent retail", "Hospitality & cafés", "Tradespeople" },
            TravelNote: "We are based in Keswick, so for local projects we can meet in person to scope your site and talk it through properly.",
            Faqs: new[]
            {
                new LocationFaq(
                    "Do you actually work in Keswick?",
                    "Yes. Fellside Digital is based in Keswick, Cumbria — not a national agency with a postcode. You deal directly with the developer building your site, and for Keswick projects we can meet face to face."),
                new LocationFaq(
                    "How much does a website for a Keswick business cost?",
                    "A static marketing website starts from £295. Sites with booking, integrations or custom features are scoped individually, and you always get a clear fixed price before work begins."),
                new LocationFaq(
                    "Can you help my guest house or holiday let take bookings online?",
                    "Yes. We build booking and availability systems with online payments and automated email confirmations, which is a common need for accommodation businesses across the Keswick and Borrowdale area."),
            }),

        new Location(
            Slug: "penrith",
            Town: "Penrith",
            PostcodeArea: "CA11",
            Latitude: 54.6646,
            Longitude: -2.7529,
            MetaTitle: "Website Design in Penrith | Web Developer | Fellside Digital",
            MetaDescription: "Bespoke website design and web development for Penrith and the Eden Valley. Local Cumbria-based developer, hand-coded sites, fixed-price quotes. No templates.",
            HeroKicker: "Website Design in Penrith",
            Lead: "Fellside Digital designs and builds bespoke websites for businesses in Penrith and the Eden Valley. We are a local, founder-led Cumbria studio — every site is hand-coded for speed and built around your business, with a fixed price agreed up front.",
            LocalContext: "Penrith sits at the gateway to the Eden Valley and the M6, which makes it a hub for agriculture, professional services, trades and independent retail. Many Penrith businesses serve a wide rural catchment, so their website has to do the heavy lifting — being found in local search, explaining the offer clearly, and turning enquiries into bookings or calls. We build sites tuned for exactly that.",
            NearbyAreas: new[] { "Eamont Bridge", "Langwathby", "Stainton", "Eden Valley", "Pooley Bridge", "Penrith CA11" },
            LocalSectors: new[] { "Agriculture & rural businesses", "Professional services", "Trades & construction", "Independent retail", "Tourism & hospitality" },
            TravelNote: "Penrith is around 30 minutes from our Keswick base, so in-person scoping meetings are easy to arrange.",
            Faqs: new[]
            {
                new LocationFaq(
                    "Are you local to Penrith?",
                    "We are based in Keswick, about half an hour from Penrith, and work with businesses across the Eden Valley. You get a genuinely local developer rather than a remote national agency."),
                new LocationFaq(
                    "I run a rural business with poor signal — will my site still be fast?",
                    "Yes. We hand-code lightweight sites rather than using bloated page builders, so they load quickly even on slower rural connections — which matters for both visitors and Google rankings."),
                new LocationFaq(
                    "Can you help my Penrith business get found in Google?",
                    "Yes. Every site we build includes the technical SEO foundations — fast loading, clean structure, local business schema and Google-friendly markup — to help you rank for local searches in and around Penrith."),
            }),

        new Location(
            Slug: "kendal",
            Town: "Kendal",
            PostcodeArea: "LA9",
            Latitude: 54.3277,
            Longitude: -2.7459,
            MetaTitle: "Website Design in Kendal | Web Developer | Fellside Digital",
            MetaDescription: "Bespoke website design and web development for Kendal and the South Lakes. Local Cumbria developer, fast hand-coded sites, fixed-price quotes. No templates.",
            HeroKicker: "Website Design in Kendal",
            Lead: "Fellside Digital builds bespoke websites for businesses in Kendal and across the South Lakes. We are a local Cumbria studio that hand-codes every site for performance — no templates — and works to a clear fixed price agreed before the project begins.",
            LocalContext: "Kendal is the commercial heart of the South Lakes — a busy market town with a strong mix of manufacturing, professional services, retail and tourism feeding off the southern entrance to the Lake District. Competition for attention is real here, so a Kendal business needs a website that is faster and sharper than the local average. We focus on exactly that: performance, clarity, and a design that holds its own.",
            NearbyAreas: new[] { "Staveley", "Burneside", "Oxenholme", "Windermere", "Kendal LA9" },
            LocalSectors: new[] { "Manufacturing & industry", "Professional services", "Retail & high street", "Tourism gateway businesses", "Hospitality" },
            TravelNote: "Kendal is an easy drive from our Keswick base for project meetings, and we work with South Lakes clients remotely day to day.",
            Faqs: new[]
            {
                new LocationFaq(
                    "Do you work with businesses in Kendal and the South Lakes?",
                    "Yes. We are a Cumbria-based studio and regularly work with businesses across Kendal and the South Lakes, combining in-person scoping with day-to-day remote delivery through a dedicated project portal."),
                new LocationFaq(
                    "Can you build more than a brochure website?",
                    "Yes. As well as marketing sites we build full web applications — client portals, dashboards, booking and e-commerce systems — which suits Kendal's manufacturing and professional-services businesses that need software, not just a website."),
                new LocationFaq(
                    "What does a Kendal website project cost?",
                    "Static marketing websites start from £295, with business sites, web applications and online stores scoped individually. You always receive a fixed, no-obligation quote before any work starts."),
            }),

        new Location(
            Slug: "carlisle",
            Town: "Carlisle",
            PostcodeArea: "CA1",
            Latitude: 54.8924,
            Longitude: -2.9320,
            MetaTitle: "Website Design in Carlisle | Web Developer | Fellside Digital",
            MetaDescription: "Bespoke website design and web development for Carlisle and north Cumbria. Local developer, fast hand-coded sites and web apps, fixed-price quotes. No templates.",
            HeroKicker: "Website Design in Carlisle",
            Lead: "Fellside Digital designs and develops bespoke websites and web applications for businesses in Carlisle and across north Cumbria. We are a local, founder-led studio building fast, hand-coded sites to a fixed price — never from a template.",
            LocalContext: "Carlisle is Cumbria's only city and its largest commercial centre, with a business base spanning logistics, retail, legal and professional services, and a growing number of ambitious SMEs. Bigger businesses often need more than a brochure site — integrations, portals, internal tools — and that is where a developer who builds proper software, not just pages, makes the difference. We do both.",
            NearbyAreas: new[] { "Stanwix", "Brampton", "Wetheral", "Dalston", "Carlisle CA1", "Carlisle CA2" },
            LocalSectors: new[] { "Logistics & distribution", "Legal & professional services", "Retail", "Manufacturing", "Growing SMEs" },
            TravelNote: "Carlisle is around 35 minutes from our Keswick base, so in-person meetings to scope larger projects are straightforward.",
            Faqs: new[]
            {
                new LocationFaq(
                    "Can you handle a larger or more complex project for a Carlisle business?",
                    "Yes. Alongside websites we build bespoke web applications — client portals, dashboards, booking and e-commerce platforms, and API integrations — which suits the larger SMEs and professional firms based in Carlisle."),
                new LocationFaq(
                    "Are you a local Carlisle web developer?",
                    "We are based in Keswick, around 35 minutes away, and work with businesses throughout Carlisle and north Cumbria. You get a local, accountable developer rather than an offshore or national agency."),
                new LocationFaq(
                    "Do you offer ongoing support after the site launches?",
                    "Yes. We offer retainer packages covering hosting, updates, backups, monitoring and priority support, so your site and any custom software keep performing long after launch."),
            }),
    };

    public static Location? FindBySlug(string? slug) =>
        slug is null ? null : All.FirstOrDefault(l => string.Equals(l.Slug, slug, StringComparison.OrdinalIgnoreCase));

    // ── JSON-LD builders ──────────────────────────────────────────────────────
    // Built in C# (not interpolated in markup) so all values are correctly
    // JSON-escaped. Each returns the inner JSON for a <script type="application/ld+json"> tag.

    private const string Origin = "https://fellsidedigital.co.uk";

    private static readonly global::System.Text.Json.JsonSerializerOptions JsonOpts = new()
    {
        Encoder = global::System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static string Json(object value) =>
        global::System.Text.Json.JsonSerializer.Serialize(value, JsonOpts);

    public static string ServiceJson(Location l) => Json(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Service",
        ["name"] = $"Website Design in {l.Town}",
        ["serviceType"] = "Web Development",
        ["description"] = l.Lead,
        ["provider"] = new Dictionary<string, object?> { ["@id"] = $"{Origin}/#business" },
        ["url"] = $"{Origin}/website-design/{l.Slug}",
        ["areaServed"] = new Dictionary<string, object?>
        {
            ["@type"] = "City",
            ["name"] = l.Town,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["addressLocality"] = l.Town,
                ["addressRegion"] = "Cumbria",
                ["addressCountry"] = "GB",
            },
            ["geo"] = new Dictionary<string, object?>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = l.Latitude,
                ["longitude"] = l.Longitude,
            },
        },
        ["offers"] = new Dictionary<string, object?>
        {
            ["@type"] = "Offer",
            ["priceCurrency"] = "GBP",
            ["priceSpecification"] = new Dictionary<string, object?>
            {
                ["@type"] = "UnitPriceSpecification",
                ["price"] = "295",
                ["priceCurrency"] = "GBP",
                ["minPrice"] = "295",
            },
        },
    });

    public static string BreadcrumbJson(Location l) => Json(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = new object[]
        {
            new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 1, ["name"] = "Home", ["item"] = $"{Origin}/" },
            new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 2, ["name"] = "Website Design", ["item"] = $"{Origin}/websites" },
            new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = 3, ["name"] = l.Town, ["item"] = $"{Origin}/website-design/{l.Slug}" },
        },
    });

    public static string FaqJson(Location l) => Json(new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "FAQPage",
        ["mainEntity"] = l.Faqs.Select(f => new Dictionary<string, object?>
        {
            ["@type"] = "Question",
            ["name"] = f.Question,
            ["acceptedAnswer"] = new Dictionary<string, object?> { ["@type"] = "Answer", ["text"] = f.Answer },
        }).ToArray(),
    });
}
