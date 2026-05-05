// animeInterop.js, FULL LIBRARY v2
// wwwroot/js/animations/animeInterop.js
//
// RULE: Never use CSS opacity:0 on elements anime.js will animate.
//       anime.js owns the initial hidden state via anime.set() or [from, to] values.
//       CSS opacity:0 on a parent silently kills all child animations.
//
// LOAD ORDER in App.razor <head>:
//   1. anime.min.js  (CDN)
//   2. animeInterop.js  (this file)

if (typeof anime === 'undefined') {
    console.error('[animeInterop] anime.js not loaded. Ensure anime.min.js is BEFORE animeInterop.js.');
}

window.fellsideAnime = {

    // ═════════════════════════════════════════════════════════════════
    // PRIVATE UTILITIES
    // ═════════════════════════════════════════════════════════════════

    _splitWords(selector) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] No element: "${selector}"`); return false; }
        el.style.opacity = '1';

        // Walk childNodes instead of using innerText so that inline elements
        // like <span class="text-accent"> have their classes preserved per word.
        // Using innerText strips all tags, losing accent colours entirely.
        const parts = [];

        el.childNodes.forEach(node => {
            if (node.nodeType === Node.TEXT_NODE) {
                // Plain text node, split into words normally
                node.textContent.trim().split(/\s+/).filter(Boolean).forEach(w => {
                    parts.push(
                        `<span style="display:inline-block;overflow:hidden;vertical-align:bottom;">` +
                        `<span class="anime-word-inner" style="display:inline-block;">${w}</span>` +
                        `</span>`
                    );
                });
            } else if (node.nodeType === Node.ELEMENT_NODE) {
                // Inline element e.g. <span class="text-accent">,
                // carry its classes onto each individual word wrapper
                const cls = node.className || '';
                node.innerText.trim().split(/\s+/).filter(Boolean).forEach(w => {
                    parts.push(
                        `<span class="${cls}" style="display:inline-block;overflow:hidden;vertical-align:bottom;">` +
                        `<span class="anime-word-inner" style="display:inline-block;">${w}</span>` +
                        `</span>`
                    );
                });
            }
        });

        el.innerHTML = parts.join('\u00A0');
        return true;
    },

    _splitChars(selector) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] No element: "${selector}"`); return false; }
        el.style.opacity = '1';
        const chars = el.innerText.trim().split('');
        el.innerHTML = chars
            .map(c => c === ' '
                ? `<span style="display:inline-block;width:0.3em;"></span>`
                : `<span class="anime-char" style="display:inline-block;">${c}</span>`)
            .join('');
        return true;
    },

    // ═════════════════════════════════════════════════════════════════
    // TEXT ANIMATIONS
    // ═════════════════════════════════════════════════════════════════

    // 1. WORDS FLOAT UP
    fadeInWords(selector, options = {}) {
        if (!this._splitWords(selector)) return;
        anime({
            targets: `${selector} .anime-word-inner`,
            translateY: [options.distance ?? 40, 0],
            opacity: [0, 1],
            duration: options.duration ?? 800,
            delay: anime.stagger(options.stagger ?? 100, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 2. WORDS SLIDE IN FROM LEFT
    fadeInWordsLeft(selector, options = {}) {
        if (!this._splitWords(selector)) return;
        anime({
            targets: `${selector} .anime-word-inner`,
            translateX: [options.distance ?? -40, 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 80, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 3. WORDS SLIDE IN FROM RIGHT
    fadeInWordsRight(selector, options = {}) {
        if (!this._splitWords(selector)) return;
        anime({
            targets: `${selector} .anime-word-inner`,
            translateX: [options.distance ?? 40, 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 80, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 4. CHARS RAIN DOWN, characters fall from above
    charsRainDown(selector, options = {}) {
        if (!this._splitChars(selector)) return;
        anime({
            targets: `${selector} .anime-char`,
            translateY: [options.distance ?? -60, 0],
            opacity: [0, 1],
            rotate: [options.rotate ?? -8, 0],
            duration: options.duration ?? 600,
            delay: anime.stagger(options.stagger ?? 50, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 5. CHARS RISE UP, characters float up from below
    charsRiseUp(selector, options = {}) {
        if (!this._splitChars(selector)) return;
        anime({
            targets: `${selector} .anime-char`,
            translateY: [options.distance ?? 50, 0],
            opacity: [0, 1],
            rotate: [options.rotate ?? 6, 0],
            duration: options.duration ?? 600,
            delay: anime.stagger(options.stagger ?? 45, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 6. TEXT SCRAMBLE, characters randomise then snap to real text
    scrambleText(selector, options = {}) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] No element: "${selector}"`); return; }
        el.style.opacity = '1';
        const original = el.innerText.trim();
        const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%';
        const duration = options.duration ?? 1200;
        const steps = options.steps ?? 18;
        let frame = 0;
        const interval = setInterval(() => {
            el.innerText = original.split('').map((c, i) => {
                if (c === ' ') return ' ';
                if (i < Math.floor((frame / steps) * original.length)) return c;
                return chars[Math.floor(Math.random() * chars.length)];
            }).join('');
            frame++;
            if (frame > steps) { el.innerText = original; clearInterval(interval); }
        }, duration / steps);
    },

    // 7. TYPEWRITER, characters appear one by one, cursor blinks at end
    typewriter(selector, options = {}) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] No element: "${selector}"`); return; }
        const original = el.innerText.trim();
        const speed = options.speed ?? 55;   // ms per character
        const showCursor = options.showCursor !== false;
        el.innerText = '';
        el.style.opacity = '1';

        if (showCursor) {
            el.style.borderRight = '2px solid currentColor';
            el.style.paddingRight = '2px';
        }

        let i = 0;
        const type = () => {
            if (i <= original.length) {
                el.innerText = original.slice(0, i);
                i++;
                setTimeout(type, speed + Math.random() * (options.jitter ?? 20));
            } else if (showCursor) {
                // Blink cursor then remove it
                let visible = true;
                const blink = setInterval(() => {
                    el.style.borderRightColor = visible ? 'currentColor' : 'transparent';
                    visible = !visible;
                }, 530);
                setTimeout(() => {
                    clearInterval(blink);
                    el.style.borderRight = 'none';
                    el.style.paddingRight = '0';
                }, options.cursorDuration ?? 2000);
            }
        };
        setTimeout(type, options.startDelay ?? 0);
    },

    // 8. BLUR CLEAR, text fades in and simultaneously un-blurs
    blurClear(selector, options = {}) {
        if (!this._splitWords(selector)) return;
        anime({
            targets: `${selector} .anime-word-inner`,
            opacity: [0, 1],
            filter: ['blur(12px)', 'blur(0px)'],
            translateY: [options.distance ?? 10, 0],
            duration: options.duration ?? 900,
            delay: anime.stagger(options.stagger ?? 90, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'easeOutQuart',
        });
    },

    // 9. FLIP IN, words flip in on the Y axis (3D card-flip feel)
    flipInWords(selector, options = {}) {
        if (!this._splitWords(selector)) return;
        const el = document.querySelector(selector);
        if (el) el.style.perspective = '600px';
        anime({
            targets: `${selector} .anime-word-inner`,
            rotateX: [options.rotateFrom ?? -90, 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 100, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // ═════════════════════════════════════════════════════════════════
    // ELEMENT / BLOCK ANIMATIONS
    // ═════════════════════════════════════════════════════════════════

    // 10. FADE UP, generic fade+rise for any block element
    fadeUp(selector, options = {}) {
        anime.set(selector, { opacity: 0, translateY: options.distance ?? 30 });
        anime({
            targets: selector,
            translateY: [options.distance ?? 30, 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 120, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 11. ZOOM IN, scale from slightly small, great for cards/images
    zoomIn(selector, options = {}) {
        anime.set(selector, { opacity: 0, scale: options.from ?? 0.85 });
        anime({
            targets: selector,
            scale: [options.from ?? 0.85, 1],
            opacity: [0, 1],
            duration: options.duration ?? 650,
            delay: anime.stagger(options.stagger ?? 110, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 12. ELASTIC POP, bouncy scale entrance, great for icons/badges/buttons
    elasticPop(selector, options = {}) {
        anime.set(selector, { opacity: 0, scale: 0 });
        anime({
            targets: selector,
            scale: [0, 1],
            opacity: [0, 1],
            duration: options.duration ?? 900,
            delay: anime.stagger(options.stagger ?? 80, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'spring(1, 80, 10, 0)',
        });
    },

    // 13. SLIDE FROM RIGHT, element(s) enter from off-screen right
    slideFromRight(selector, options = {}) {
        anime.set(selector, { opacity: 0, translateX: options.distance ?? 60 });
        anime({
            targets: selector,
            translateX: [options.distance ?? 60, 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 100, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 14. SLIDE FROM LEFT, element(s) enter from off-screen left
    slideFromLeft(selector, options = {}) {
        anime.set(selector, { opacity: 0, translateX: options.distance ?? -60 });
        anime({
            targets: selector,
            translateX: [-(options.distance ?? 60), 0],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: anime.stagger(options.stagger ?? 100, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'cubicBezier(0.22, 1, 0.36, 1)',
        });
    },

    // 15. STAGGERED CHILDREN REVEAL
    staggeredReveal(selector, options = {}) {
        anime.set(`${selector} > *`, { opacity: 0, translateY: options.distance ?? 20 });
        anime({
            targets: `${selector} > *`,
            translateY: [options.distance ?? 20, 0],
            opacity: [0, 1],
            duration: options.duration ?? 600,
            delay: anime.stagger(options.stagger ?? 90, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'easeOutQuart',
        });
    },

    // 16. COUNTER, fades element in AND ticks number up
    countUp(selector, target, options = {}) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] No element: "${selector}"`); return; }
        const suffix = options.suffix ?? '';
        anime({ targets: el, opacity: [0, 1], duration: 400, easing: 'easeOutQuart' });
        const obj = { val: 0 };
        anime({
            targets: obj,
            val: target,
            round: 1,
            duration: options.duration ?? 1800,
            easing: options.easing ?? 'easeOutExpo',
            update: () => { el.innerText = obj.val.toLocaleString() + suffix; }
        });
    },

    // 17. PROGRESS BAR, animates a bar element from 0 → target width %
    //     Target element should be a div with a fixed height and bg colour
    progressBar(selector, targetPercent, options = {}) {
        anime.set(selector, { width: '0%', opacity: 1 });
        anime({
            targets: selector,
            width: `${targetPercent}%`,
            duration: options.duration ?? 1200,
            delay: options.startDelay ?? 0,
            easing: options.easing ?? 'easeOutQuart',
        });
    },

    // 18. UNDERLINE DRAW
    drawUnderline(selector, options = {}) {
        anime({
            targets: selector,
            width: ['0%', '100%'],
            opacity: [0, 1],
            duration: options.duration ?? 700,
            delay: options.startDelay ?? 400,
            easing: options.easing ?? 'easeOutQuart',
        });
    },

    // 19. PULSE, repeating scale pulse, great for CTAs or live indicators
    pulse(selector, options = {}) {
        anime({
            targets: selector,
            scale: [1, options.scale ?? 1.06, 1],
            duration: options.duration ?? 900,
            delay: anime.stagger(options.stagger ?? 0, { start: options.startDelay ?? 0 }),
            easing: options.easing ?? 'easeInOutSine',
            loop: options.loop ?? true,
            direction: 'alternate',
        });
    },

    // 20. FLOAT, gentle up/down hover loop, great for hero icons
    float(selector, options = {}) {
        anime({
            targets: selector,
            translateY: [0, options.distance ?? -10, 0],
            duration: options.duration ?? 2400,
            easing: options.easing ?? 'easeInOutSine',
            loop: true,
            delay: anime.stagger(options.stagger ?? 200),
        });
    },

    // 21. TIMELINE SEQUENCE, orchestrate multiple animations in order
    //     Pass array of { selector, effect, options, offset } objects
    //     offset: ms after previous step starts (default 0 = sequential)
    //
    //     Example from Blazor:
    //     await JS.InvokeVoidAsync("fellsideAnime.timeline", new[] {
    //         new { selector="#h1", effect="fadeInWords",  options=new{}, offset=0   },
    //         new { selector="#p",  effect="fadeUp",       options=new{}, offset=400 },
    //     });
    timeline(steps) {
        const tl = anime.timeline({ easing: 'easeOutQuart' });
        (steps || []).forEach(step => {
            const fn = this[step.effect];
            if (!fn) { console.warn(`[animeInterop] timeline: unknown effect "${step.effect}"`); return; }
            // For effects that need special handling (text split etc) call directly
            // Timeline offset controls sequencing
            tl.add({
                targets: step.selector,
                opacity: [0, 1],
                translateY: [20, 0],
                duration: step.duration ?? 700,
                ...step.animeProps,
            }, step.offset ?? '+=0');
        });
    },

    // ═════════════════════════════════════════════════════════════════
    // INTERSECTION OBSERVER, fire callback when element scrolls in
    // ═════════════════════════════════════════════════════════════════

    onVisible(selector, callback, threshold = 0.15) {
        const el = document.querySelector(selector);
        if (!el) { console.warn(`[animeInterop] onVisible: No element: "${selector}"`); return; }
        const obs = new IntersectionObserver(([entry]) => {
            if (entry.isIntersecting) {
                if (typeof callback === 'function') callback();
                else if (typeof window[callback] === 'function') window[callback]();
                obs.disconnect();
            }
        }, { threshold });
        obs.observe(el);
    },

    // ═════════════════════════════════════════════════════════════════
    // SCROLL-TRIGGERED WRAPPERS, OnScroll suffix versions of everything
    // ═════════════════════════════════════════════════════════════════

    fadeInWordsOnScroll(selector, options = {}) { this.onVisible(selector, () => this.fadeInWords(selector, options)); },
    fadeInWordsLeftOnScroll(selector, options = {}) { this.onVisible(selector, () => this.fadeInWordsLeft(selector, options)); },
    fadeInWordsRightOnScroll(selector, options = {}) { this.onVisible(selector, () => this.fadeInWordsRight(selector, options)); },
    charsRainDownOnScroll(selector, options = {}) { this.onVisible(selector, () => this.charsRainDown(selector, options)); },
    charsRiseUpOnScroll(selector, options = {}) { this.onVisible(selector, () => this.charsRiseUp(selector, options)); },
    blurClearOnScroll(selector, options = {}) { this.onVisible(selector, () => this.blurClear(selector, options)); },
    flipInWordsOnScroll(selector, options = {}) { this.onVisible(selector, () => this.flipInWords(selector, options)); },
    typewriterOnScroll(selector, options = {}) { this.onVisible(selector, () => this.typewriter(selector, options)); },
    fadeUpOnScroll(selector, options = {}) { this.onVisible(selector, () => this.fadeUp(selector, options)); },
    zoomInOnScroll(selector, options = {}) { this.onVisible(selector, () => this.zoomIn(selector, options)); },
    elasticPopOnScroll(selector, options = {}) { this.onVisible(selector, () => this.elasticPop(selector, options)); },
    slideFromRightOnScroll(selector, options = {}) { this.onVisible(selector, () => this.slideFromRight(selector, options)); },
    slideFromLeftOnScroll(selector, options = {}) { this.onVisible(selector, () => this.slideFromLeft(selector, options)); },
    staggeredRevealOnScroll(selector, options = {}) { this.onVisible(selector, () => this.staggeredReveal(selector, options)); },
    countUpOnScroll(selector, target, options = {}) { this.onVisible(selector, () => this.countUp(selector, target, options)); },
    progressBarOnScroll(selector, pct, options = {}) { this.onVisible(selector, () => this.progressBar(selector, pct, options)); },
};