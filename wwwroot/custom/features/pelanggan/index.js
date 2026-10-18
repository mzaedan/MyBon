/* Pelanggan > Index: empty-state Lottie, live search, and server-paged card loading. */
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
        var container = stack.querySelector('.pelanggan-cards');
        // The server ships the total with every render, so the title keeps reporting every match
        // instead of only the cards painted so far.
        var total = container ? parseInt(container.getAttribute('data-total'), 10) || 0 : 0;
        title.textContent = total + (input.value.trim() ? ' nama cocok' : ' nama di buku');
        title.classList.toggle('pelanggan-hidden', total === 0);
    }

    function clearLoadError() {
        var error = stack.querySelector('.pelanggan-load-more-error');
        if (error) {
            error.remove();
        }
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

        var request = $.ajax({
            url: searchUrl,
            method: 'GET',
            data: { q: input.value.trim() },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        pending = request;

        request
            .done(renderCards)
            .fail(function (xhr, status) {
                if (status !== 'abort') {
                    renderCards('<p class="pelanggan-search-empty">Gagal memuat hasil pencarian. Coba lagi.</p>');
                }
            })
            .always(function () {
                // Only the newest request clears the slot: an aborted one answers after its successor exists.
                if (pending === request) {
                    pending = null;
                }
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

        // The server owns the page number: every render ships a button pointing at the page after it.
        var nextPage = parseInt(button.getAttribute('data-next-page'), 10);
        if (!nextPage) {
            return;
        }

        if (pending) {
            // A newer keystroke already claimed the stack, so this page would land on results it no longer shows.
            pending.abort();
        }

        var request = $.ajax({
            url: searchUrl,
            method: 'GET',
            data: { q: input.value.trim(), page: nextPage },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        pending = request;

        request
            .done(function (html) {
                // Parsed detached, so the wrapper the response carries cannot duplicate the ids already on the page.
                var response = $('<div>').html(html);
                var incoming = response.find('.pelanggan-cards').html();
                if (incoming) {
                    container.insertAdjacentHTML('beforeend', incoming);
                }

                clearLoadError();

                // No button in the response means the server has no page after this one.
                var next = response.find('.pelanggan-load-more');
                if (next.length) {
                    button.replaceWith(next[0]);
                } else {
                    button.remove();
                }
            })
            .fail(function (xhr, status) {
                if (status !== 'abort') {
                    clearLoadError();
                    // The button stays put, so the same page can be asked for again.
                    button.insertAdjacentHTML('afterend',
                        '<p class="pelanggan-search-empty pelanggan-load-more-error">Gagal memuat nama berikutnya. Coba lagi.</p>');
                }
            })
            .always(function () {
                if (pending === request) {
                    pending = null;
                }
            });
    });
});
