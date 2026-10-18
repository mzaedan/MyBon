/* Barang > Delete: trash Lottie animation on the delete confirmation screen. */
document.addEventListener('DOMContentLoaded', function () {
    var animContainer = document.getElementById('lottie-trash');
    if (!animContainer) {
        return;
    }

    lottie.loadAnimation({
        container: animContainer,
        renderer: 'svg',
        loop: true,
        autoplay: true,
        path: '/lottie/Trash.json'
    });
});
