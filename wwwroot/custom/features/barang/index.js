/* Barang > Index: notebook Lottie for the empty stock list, plus progressive card reveal. */
document.addEventListener('DOMContentLoaded', function () {
    var animContainer = document.getElementById('lottie-barang-empty');
    if (animContainer) {
        lottie.loadAnimation({
            container: animContainer,
            renderer: 'svg',
            loop: true,
            autoplay: true,
            path: '/lottie/Notebook with Pencil Icon Animation.json'
        });
    }

    var stack = document.getElementById('barang-card-stack');
    if (!stack) {
        return;
    }

    // Delegated on the stack, like the Pelanggan list, so the handler survives any re-render of the cards.
    stack.addEventListener('click', function (event) {
        var button = event.target.closest('.barang-load-more');
        var container = stack.querySelector('.barang-cards');
        if (!button || !container) {
            return;
        }

        var pageSize = parseInt(container.getAttribute('data-page-size'), 10) || 8;
        var hiddenCards = container.querySelectorAll('.barang-card--hidden');
        for (var i = 0; i < pageSize && i < hiddenCards.length; i++) {
            hiddenCards[i].classList.remove('barang-card--hidden');
        }

        if (!container.querySelector('.barang-card--hidden')) {
            button.remove();
        }
    });
});
