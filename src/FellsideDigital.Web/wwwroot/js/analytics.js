// Anonymous, consent-gated visitor analytics.
// Nothing here runs — and no beacon is sent — until the visitor clicks Accept.
// No cookies beyond the consent choice are set unless consent is granted.
(function () {
    "use strict";

    var CONSENT_COOKIE = "fd_consent";
    var SESSION_COOKIE = "fd_sid";
    var ONE_YEAR = 60 * 60 * 24 * 365;
    var SIX_MONTHS = 60 * 60 * 24 * 182;

    function getCookie(name) {
        var m = document.cookie.match("(?:^|; )" + name + "=([^;]*)");
        return m ? decodeURIComponent(m[1]) : null;
    }

    function setCookie(name, value, maxAgeSeconds) {
        var secure = location.protocol === "https:" ? "; Secure" : "";
        document.cookie = name + "=" + encodeURIComponent(value) +
            "; Max-Age=" + maxAgeSeconds + "; Path=/; SameSite=Lax" + secure;
    }

    function choice() {
        return getCookie(CONSENT_COOKIE); // "accepted" | "rejected" | null
    }

    function sessionId() {
        var id = getCookie(SESSION_COOKIE);
        if (!id) {
            id = (crypto.randomUUID && crypto.randomUUID()) ||
                ("xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, function (c) {
                    var r = (Math.random() * 16) | 0;
                    return (c === "x" ? r : (r & 0x3) | 0x8).toString(16);
                }));
            setCookie(SESSION_COOKIE, id, SIX_MONTHS);
        }
        return id;
    }

    function utm(name) {
        try { return new URLSearchParams(location.search).get(name); } catch (e) { return null; }
    }

    // Engagement tracking (only meaningful once consented).
    var startedAt = Date.now();
    var maxScroll = 0;
    window.addEventListener("scroll", function () {
        var doc = document.documentElement;
        var scrollable = doc.scrollHeight - doc.clientHeight;
        if (scrollable > 0) {
            var pct = Math.round((doc.scrollTop / scrollable) * 100);
            if (pct > maxScroll) maxScroll = Math.min(100, pct);
        }
    }, { passive: true });

    function buildPayload() {
        return {
            sessionId: sessionId(),
            path: location.pathname,
            language: navigator.language || null,
            timezone: (Intl.DateTimeFormat().resolvedOptions().timeZone) || null,
            screenWidth: screen.width || null,
            screenHeight: screen.height || null,
            viewportWidth: window.innerWidth || null,
            viewportHeight: window.innerHeight || null,
            referrer: document.referrer || null,
            utmSource: utm("utm_source"),
            utmMedium: utm("utm_medium"),
            utmCampaign: utm("utm_campaign"),
            engagementSeconds: Math.round((Date.now() - startedAt) / 1000),
            scrollDepthPercent: maxScroll
        };
    }

    var sent = false;
    function send() {
        if (sent || choice() !== "accepted") return;
        sent = true;
        var body = JSON.stringify(buildPayload());
        // Prefer sendBeacon so it survives page unload; fall back to fetch.
        try {
            if (navigator.sendBeacon) {
                navigator.sendBeacon("/api/analytics/visit", new Blob([body], { type: "application/json" }));
                return;
            }
        } catch (e) { /* fall through */ }
        fetch("/api/analytics/visit", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: body,
            keepalive: true
        }).catch(function () { });
    }

    function startCapture() {
        // Send a final event when the visitor leaves (captures engagement + scroll).
        window.addEventListener("pagehide", send, { once: false });
        document.addEventListener("visibilitychange", function () {
            if (document.visibilityState === "hidden") send();
        });
    }

    // If consent was granted on a previous visit, resume capturing immediately.
    if (choice() === "accepted") {
        startCapture();
    }

    window.fdConsent = {
        choice: choice,
        accept: function () {
            setCookie(CONSENT_COOKIE, "accepted", ONE_YEAR);
            // Capture starts now; this pageview is beaconed when the visitor leaves the
            // page (once), so we record engagement without double-counting.
            startCapture();
        },
        reject: function () {
            setCookie(CONSENT_COOKIE, "rejected", ONE_YEAR);
        }
    };

    // --- Consent banner -----------------------------------------------------
    // Injected only when the visitor has not yet chosen. Inline styles keep it
    // independent of the compiled Tailwind bundle and let it paint immediately.
    function renderBanner() {
        if (choice() !== null || document.getElementById("fd-consent-banner")) return;

        var dark = window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches;
        var bg = dark ? "#171717" : "#ffffff";
        var fg = dark ? "#e5e5e5" : "#1f2937";
        var muted = dark ? "#a3a3a3" : "#6b7280";
        var border = dark ? "#2f2f2f" : "#e5e7eb";

        var el = document.createElement("div");
        el.id = "fd-consent-banner";
        el.setAttribute("role", "dialog");
        el.setAttribute("aria-label", "Cookie consent");
        el.style.cssText =
            "position:fixed;left:16px;right:16px;bottom:16px;z-index:2147483000;max-width:640px;" +
            "margin:0 auto;background:" + bg + ";color:" + fg + ";border:1px solid " + border + ";" +
            "border-radius:14px;padding:18px 20px;box-shadow:0 10px 40px rgba(0,0,0,.18);" +
            "font-family:system-ui,-apple-system,Segoe UI,Roboto,sans-serif;font-size:14px;line-height:1.5;";

        el.innerHTML =
            '<p style="margin:0 0 12px 0;color:' + fg + '">' +
            'We use cookies to understand how visitors use our site — including your device type, ' +
            'approximate location, and how you got here. This is anonymous and helps us improve. ' +
            '<a href="/privacy" style="color:#0ea5e9;text-decoration:underline">Privacy notice</a>.' +
            '</p>' +
            '<div style="display:flex;gap:10px;flex-wrap:wrap">' +
            '<button id="fd-consent-accept" style="cursor:pointer;border:0;border-radius:9px;' +
            'padding:9px 18px;font-weight:600;font-size:14px;background:#0ea5e9;color:#fff">Accept</button>' +
            '<button id="fd-consent-reject" style="cursor:pointer;border:1px solid ' + border + ';' +
            'border-radius:9px;padding:9px 18px;font-weight:600;font-size:14px;background:transparent;color:' +
            muted + '">Reject</button>' +
            '</div>';

        document.body.appendChild(el);

        function close() { el.parentNode && el.parentNode.removeChild(el); }
        document.getElementById("fd-consent-accept").addEventListener("click", function () {
            window.fdConsent.accept(); close();
        });
        document.getElementById("fd-consent-reject").addEventListener("click", function () {
            window.fdConsent.reject(); close();
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", renderBanner);
    } else {
        renderBanner();
    }
})();
