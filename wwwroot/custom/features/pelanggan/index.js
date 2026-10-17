/* Pelanggan > Index: empty-state Lottie, live search, and progressive card reveal. */
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

    var stack = document.getElementById('pelanggan-card-stack');
    var title = document.getElementById('pelanggan-list-title');
    var input = document.getElementById('pelanggan-search-input');
    if (!stack || !title || !input) {
        return;
    }

    var searchUrl = stack.getAttribute('data-search-url');
    var debounceMs = 250;
    var timer = null;
    var pending = null;

    function updateTitle() {
        var total = stack.querySelectorAll('.pelanggan-card').length;
        title.textContent = total + (input.value.trim() ? ' nama cocok' : ' nama di buku');
        title.classList.toggle('pelanggan-hidden', total === 0);
    }

    function renderCards(html) {
        // Only the card stack is replaced, so the search input keeps focus and its text.
        stack.innerHTML = html;
        updateTitle();
    }

    function search() {
        if (pending) {
            // Drop the in-flight response: a newer keystroke already asked for the latest term.
            pending.abort();
        }

        pending = $.ajax({
            url: searchUrl,
            method: 'GET',
            data: { q: input.value.trim() },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .done(renderCards)
            .fail(function (xhr, status) {
                if (status !== 'abort') {
                    renderCards('<p class="pelanggan-search-empty">Gagal memuat hasil pencarian. Coba lagi.</p>');
                }
            })
            .always(function () {
                pending = null;
            });
    }

    input.addEventListener('input', function () {
        window.clearTimeout(timer);
        timer = window.setTimeout(search, debounceMs);
    });

    if (input.form) {
        // With scripting on, the list refreshes in place instead of reloading on Enter.
        input.form.addEventListener('submit', function (event) {
            event.preventDefault();
            window.clearTimeout(timer);
            search();
        });
    }

    // Delegated on the stack so it survives every search re-render.
    stack.addEventListener('click', function (event) {
        var button = event.target.closest('.pelanggan-load-more');
        var container = stack.querySelector('.pelanggan-cards');
        if (!button || !container) {
            return;
        }

        var pageSize = parseInt(container.getAttribute('data-page-size'), 10) || 8;
        var hiddenCards = container.querySelectorAll('.pelanggan-card--hidden');
        for (var i = 0; i < pageSize && i < hiddenCards.length; i++) {
            hiddenCards[i].classList.remove('pelanggan-card--hidden');
        }

        if (!container.querySelector('.pelanggan-card--hidden')) {
            button.remove();
        }
    });
});
