window.heroCarousel = {
    // Attempt to load an iframe; fall back to the screenshot/mockup element if
    // the site refuses framing (X-Frame-Options / CSP frame-ancestors) or never loads.
    tryLoadIframe(iframeId, fallbackId) {
        const iframe = document.getElementById(iframeId);
        const fallback = document.getElementById(fallbackId);
        if (!iframe) return;

        // Show the fallback immediately so there's no blank gap while loading.
        if (fallback) fallback.style.display = 'flex';
        iframe.style.display = 'none';

        // If no src set, stay on the fallback.
        if (!iframe.src || iframe.src === window.location.href) return;

        // Give the site up to 6s to load; if `load` never fires (commonly how a
        // framing refusal manifests), keep the fallback.
        const deadline = setTimeout(showFallback, 6000);

        iframe.addEventListener('load', () => {
            clearTimeout(deadline);
            try {
                // Same-origin pages: we can introspect, so verify they actually rendered.
                const doc = iframe.contentDocument || iframe.contentWindow?.document;
                if (doc && (!doc.body || doc.body.innerHTML.trim() === '')) {
                    showFallback();
                    return;
                }
                // doc readable + non-empty → success (falls through to showIframe).
            } catch {
                // Cross-origin (the normal case for an external client site): the
                // browser blocks introspection, but `load` firing means the frame
                // embedded successfully. Treat as success.
            }
            showIframe();
        }, { once: true });

        iframe.addEventListener('error', () => {
            clearTimeout(deadline);
            showFallback();
        }, { once: true });

        function showIframe() {
            iframe.style.display = 'block';
            if (fallback) fallback.style.display = 'none';
        }

        function showFallback() {
            iframe.style.display = 'none';
            if (fallback) fallback.style.display = 'flex';
        }
    }
};
