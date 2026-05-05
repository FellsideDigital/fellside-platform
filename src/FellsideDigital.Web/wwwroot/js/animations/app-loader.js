window.appLoader = {
    init: function (el) {
        anime.timeline({ easing: 'easeOutExpo' })
            .add({
                targets: el.querySelector('.loader-title'),
                translateY: [20, 0],
                opacity: [0, 1],
                duration: 900
            })
            .add({
                targets: el.querySelector('.loader-underline'),
                width: ['0%', '100%'],
                opacity: [0, 1],
                duration: 800
            }, '-=600')
            .add({
                targets: el.querySelector('.loader-progress'),
                width: ['0%', '100%'],
                duration: 1800,
                easing: 'easeInOutQuad'
            }, '-=500');
    },

    hide: function (el) {
        anime({
            targets: el,
            opacity: [1, 0],
            duration: 600,
            easing: 'easeOutQuad',
            complete: function () {
                el.style.display = 'none';
            }
        });
    }
};