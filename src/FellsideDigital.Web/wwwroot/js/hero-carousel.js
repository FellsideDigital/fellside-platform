window.heroCarousel = {
    // Preview keys (project ids) whose iframe has successfully fired `load`.
    _loaded: new Set(),

    // Wired via the iframe's inline onload attribute, so it is registered
    // before the iframe can possibly finish loading — no race with Blazor's
    // OnAfterRenderAsync interop, which may run after `load` has fired.
    onIframeLoad(iframe) {
        const key = iframe.dataset.previewKey || iframe.id;
        this._loaded.add(key);
        this._swap(iframe, document.getElementById(iframe.dataset.fallbackId), true);
    },

    // Called from Blazor after each render. Idempotent: if this preview
    // already loaded (now or on an earlier visit to this slide), show it
    // immediately; otherwise leave the fallback up and give the site 6s.
    tryLoadIframe(iframeId, fallbackId) {
        const iframe = document.getElementById(iframeId);
        const fallback = document.getElementById(fallbackId);
        if (!iframe) return;

        const key = iframe.dataset.previewKey || iframeId;

        if (this._loaded.has(key)) {
            this._swap(iframe, fallback, true);
            return;
        }

        this._swap(iframe, fallback, false);

        // Framing refusals (X-Frame-Options / CSP) usually never fire `load`:
        // keep the fallback if nothing arrives in time. Re-resolve the node
        // and key at fire time rather than trusting this closure — Blazor may
        // have reused/repurposed these DOM nodes for a different slide by
        // the time this timer fires.
        setTimeout(() => {
            const el = document.getElementById(iframeId);
            if (!el || el.dataset.previewKey !== key) return; // slide changed — stale timer
            if (!this._loaded.has(key)) this._swap(el, document.getElementById(el.dataset.fallbackId), false);
        }, 6000);
    },

    _swap(iframe, fallback, showIframe) {
        iframe.style.display = showIframe ? 'block' : 'none';
        if (fallback) fallback.style.display = showIframe ? 'none' : 'flex';
    }
};
