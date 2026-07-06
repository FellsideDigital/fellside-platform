# SEO — owner actions (outside code)

Verified in code on 2026-07-05: meta/canonical/OG on all marketing pages,
JSON-LD (LocalBusiness, Service, Breadcrumb, FAQ) valid, sitemap ↔ route parity,
robots.txt correct, llms.txt present, default og:image set.

Things only the site owner can do:

1. **Google Search Console** — verify the property and paste the verification
   token into `src/FellsideDigital.Web/Components/App.razor` (the commented-out
   `google-site-verification` meta, ~line 30). Then submit `sitemap.xml`.
2. **Google Business Profile** — create/claim the listing for Fellside Digital
   (Keswick, Cumbria); it feeds the local-pack results the
   `/website-design/{town}` pages target.
3. **Bing Webmaster Tools** — import from Search Console once (1) is done.
4. After any future page addition: add it to `wwwroot/sitemap.xml` and bump
   `<lastmod>`.
