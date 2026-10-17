/* Pelanggan > Index: empty-state Lottie animation and progressive "load more" card reveal. */
document.addEventListener('DOMContentLoaded', function () {
    var animContainer = document.getElementById('lottie-notebook');
    if (animContainer) {
        lottie.loadAnimation({
            container: animContainer,
            renderer: 'svg',
            loop: true,
            autoplay: true,
            path: '/lottie/Notebook with Pencil Icon Animation.json'
        });
    }

    var loadMore = document.getElementById('pelanggan-load-more');
    if (!loadMore) {
        return;
    }

    var pageSize = 8;
    loadMore.addEventListener('click', function () {
        var hiddenCards = document.querySelectorAll('.pelanggan-card--hidden');
        for (var i = 0; i < pageSize && i < hiddenCards.length; i++) {
            hiddenCards[i].classList.remove('pelanggan-card--hidden');
        }

        if (!document.querySelector('.pelanggan-card--hidden')) {
            loadMore.remove();
        }
    });
});
