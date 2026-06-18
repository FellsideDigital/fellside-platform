# Local + AI SEO — your action checklist

This is the list of things **only you can do** (they need your Google account, your real
business details, or a live deployment). The code side — schema, location pages, sitemap,
`llms.txt`, `pricing.txt` — is already done in the repo.

Last updated: 2026-06-18

---

## 1. Google Business Profile (Maps + local "map pack") — highest priority

You said a profile already exists. Optimise it — this is the single biggest lever for
ranking in Google Maps and the local pack for "web developer near me", "website design
Keswick", etc.

- [ ] **Primary category:** set to **Website designer**. Add secondary categories:
      *Web developer*, *Software company*, *Marketing agency* (whichever fit).
- [ ] **Name:** exactly `Fellside Digital` — no keyword stuffing (e.g. not "Fellside Digital
      Website Design Keswick"; Google can suspend for that).
- [ ] **Address / service area:** if you don't take customers at an address, set it as a
      **service-area business** and list Keswick, Penrith, Kendal, Carlisle + "Cumbria".
- [ ] **Phone + website:** must match the site exactly — `+44 7484 323505`,
      `https://fellsidedigital.co.uk`.
- [ ] **Hours:** Mon–Fri 09:00–17:30 (matches the site schema).
- [ ] **Description:** reuse the `llms.txt` summary.
- [ ] **Photos:** logo + 5–10 real photos (work, screenshots, you). Profiles with photos
      get materially more clicks.
- [ ] **Services:** add each service (website design, web apps, e-commerce, automation).
- [ ] **Reviews:** ask every happy client for a Google review and reply to each one.
      Review count + recency is a top-3 local ranking factor.
- [ ] **Google Posts:** post monthly (offers, new work) — keeps the profile "active".

## 2. Make the site's NAP match the profile exactly

NAP = Name, Address, Phone. Inconsistency across the web dilutes local ranking.

- [ ] In `src/FellsideDigital.Web/Components/Pages/Marketing/Home.razor`, the
      `ProfessionalService` schema has a maintainer comment listing fields to add:
      - `streetAddress` + `postalCode` (only if you list a real address on GBP)
      - `hasMap` → your Google Maps place URL
      - `sameAs` → your LinkedIn / Facebook / Instagram / GBP URLs
      Fill these with the **exact** same values as the profile.

## 3. Google Search Console (can't track/submit without this)

- [ ] Verify the domain in [Search Console](https://search.google.com/search-console).
- [ ] Paste the verification token into `src/FellsideDigital.Web/Components/App.razor`
      (line ~30 has a commented `<meta name="google-site-verification" ...>` ready).
- [ ] Submit the sitemap: `https://fellsidedigital.co.uk/sitemap.xml`.
- [ ] After deploy, run the 4 location pages + home through the
      [Rich Results Test](https://search.google.com/test/rich-results) to confirm the
      schema validates.

## 4. Local citations / directories (off-site authority for local ranking)

Get listed with **identical NAP** on:
- [ ] Bing Places, Apple Business Connect
- [ ] Yell, Thomson Local, Cylex, FreeIndex, Scoot (UK)
- [ ] Cumbria Chamber of Commerce / local business directories
- [ ] LinkedIn company page

## 5. AI SEO follow-ups (getting cited by ChatGPT / Perplexity / AI Overviews)

The on-site work (FAQ schema, `llms.txt`, `pricing.txt`, extractable copy) is done. The
rest is presence + freshness, which lives off your own domain:
- [ ] Get mentioned on third-party pages (local press, directories, client case-study
      backlinks) — AI cites third-party sources ~6.5× more than your own site.
- [ ] Add real client work / case studies as you go — original detail gets cited.
- [ ] Re-check monthly: search ChatGPT/Perplexity for "web developer in Cumbria",
      "website designer Keswick" and note whether you're cited.

## 6. Deploy

None of the above ranks until the new pages are live. Deploy, then do steps 1–3.

---

### What's already in the codebase (no action needed)

- Per-town landing pages: `/website-design/{keswick|penrith|kendal|carlisle}`
- `Service` + `BreadcrumbList` + `FAQPage` schema on each town page
- `ProfessionalService` schema upgraded with `geo`, `logo`, `image`
- Hub links from `/websites` → town pages (and town pages cross-link)
- `llms.txt` and `pricing.txt` for AI agents
- `sitemap.xml` updated with town pages + `lastmod`
