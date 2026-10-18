/* Barang > Index: notebook Lottie for the empty stock list. */
document.addEventListener('DOMContentLoaded', function () {
    var animContainer = document.getElementById('lottie-barang-empty');
    if (!animContainer) {
        return;
    }

    lottie.loadAnimation({
        container: animContainer,
        renderer: 'svg',
        loop: true,
        autoplay: true,
        path: '/lottie/Notebook with Pencil Icon Animation.json'
    });
});
