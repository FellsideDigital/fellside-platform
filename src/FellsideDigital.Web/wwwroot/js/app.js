function triggerScramble() {
    fellsideAnime.scrambleText('#e4-heading', {
        duration: 1200,
        steps: 18
    });
}

window.fellsideScroll = (() => {

    let cards = [];
    let isMobile = window.matchMedia("(max-width: 768px)").matches;

    const MOBILE_QUERY = window.matchMedia("(max-width: 768px)");

    /* ---------- Card Setup ---------- */

    function initCards() {
        cards = [...document.querySelectorAll(".scroll-card")];

        cards.forEach(card => {

            const ripple = card.querySelector(".ripple");

            /* Click → ripple → navigate */
            card.addEventListener("click", () => {
                if (ripple) {
                    ripple.style.transform = "scale(1.8)";
                    ripple.style.opacity = "1";

                    setTimeout(() => {
                        ripple.style.transform = "scale(0)";
                        ripple.style.opacity = "0";
                    }, 600);
                }

                if (card.dataset.link) {
                    setTimeout(() => {
                        window.location.href = card.dataset.link;
                    }, 750);
                }
            });

            /* Mouse position for glow */
            card.addEventListener("mousemove", e => {
                const rect = card.getBoundingClientRect();
                card.style.setProperty("--x", `${e.clientX - rect.left}px`);
                card.style.setProperty("--y", `${e.clientY - rect.top}px`);
            });

        });

        animateOnScroll();
    }

    /* ---------- Scroll Animation ---------- */

    function animateOnScroll() {
        if (isMobile) return;

        const vh = window.innerHeight;
        const start = vh * 0.9;
        const end = vh * 0.25;

        cards.forEach(card => {
            const rect = card.getBoundingClientRect();

            let progress = (start - rect.top) / (start - end);
            progress = Math.min(Math.max(progress, 0), 1);

            const translateY = 80 * (1 - progress);
            const scale = 0.97 + (0.03 * progress);

            card.style.transform =
                `translateY(${translateY}px) scale(${scale})`;

            card.style.opacity =
                rect.top <= vh * 0.5 ? 1 : progress;
        });
    }

    /* ---------- Smooth Scroll Helper ---------- */

    function toOfferings() {
        document
            .getElementById("offerings")
            ?.scrollIntoView({ behavior: "smooth" });
    }

    /* ---------- Init ---------- */

    function init() {
        initCards();

        window.addEventListener("scroll", animateOnScroll);
        window.addEventListener("load", animateOnScroll);

        MOBILE_QUERY.addEventListener("change", e => {
            isMobile = e.matches;
            animateOnScroll();
        });
    }

    return {
        init,
        toOfferings,
        animateOnScroll
    };

})();

window.fellsideTheme = {
    init() {
        const s = localStorage.getItem('fellside-theme') ?? 'dark';
        document.documentElement.classList.toggle('dark', s === 'dark');
        this.updateFavicon(s);
    },
    toggle() {
        const isDark = document.documentElement.classList.toggle('dark');
        const theme = isDark ? 'dark' : 'light';
        localStorage.setItem('fellside-theme', theme);
        this.updateFavicon(theme);
    },
    updateFavicon(theme) {
        const link = document.querySelector("link[rel*='icon']") || document.createElement('link');
        link.type = 'image/x-icon';
        link.rel = 'icon';
        link.href = theme === 'dark' ? '/favicon-dark.ico' : '/favicon.ico';
        document.getElementsByTagName('head')[0].appendChild(link);
    }
};

window.fellsideTheme.init();