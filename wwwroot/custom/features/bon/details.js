/* Bon > Details: the Catat pembayaran sheet with its Tanggal calendar (drawn by the shared
   helpers/bon-calendar.js), the Tambah barang sheet, and the stock rows that sheet lists. Picking a
   barang submits #bonAddForm, so the line is written and this page repaints from the notebook. The
   payment sheet is still visual: Simpan only closes it. */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var shell = document.querySelector('.app-shell');
    if (!shell) {
        return;
    }

    var openSheet = null;

    function createSheet(config) {
        var sheet = document.getElementById(config.sheetId);
        var trigger = document.getElementById(config.triggerId);
        if (!sheet || !trigger) {
            return null;
        }

        // The overlays are absolute, so they live on the shell instead of inside the scrolling main.
        shell.appendChild(sheet);

        var panel = sheet.querySelector(config.panelSelector);
        var lastFocus = null;

        function innerScope() {
            return config.getFocusScope ? config.getFocusScope() : null;
        }

        function focusables(scope) {
            return Array.prototype.slice.call(
                (scope || panel).querySelectorAll(
                    'button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [href]'
                )
            );
        }

        function close() {
            if (sheet.hidden) {
                return;
            }

            sheet.hidden = true;
            sheet.classList.remove('is-open');
            trigger.setAttribute('aria-expanded', 'false');

            if (openSheet === controller) {
                openSheet = null;
            }

            if (config.onClose) {
                config.onClose();
            }

            if (lastFocus) {
                lastFocus.focus();
            }
        }

        function open() {
            lastFocus = trigger;

            // Only one scrim at a time: opening this sheet dismisses the other.
            if (openSheet && openSheet !== controller) {
                openSheet.close();
            }

            sheet.hidden = false;
            sheet.classList.add('is-open');
            trigger.setAttribute('aria-expanded', 'true');
            openSheet = controller;

            var initial = config.initialFocusId
                ? document.getElementById(config.initialFocusId)
                : null;
            (initial || panel).focus();
        }

        var controller = { close: close };

        trigger.addEventListener('click', open);

        sheet.addEventListener('click', function (event) {
            if (event.target.closest(config.closeSelector)) {
                close();
                return;
            }

            var picked = config.pickSelector ? event.target.closest(config.pickSelector) : null;
            if (!picked) {
                return;
            }

            // The caller decides what a pick means: the add sheet writes it, the rest just close.
            if (config.onPick) {
                config.onPick(picked);
            } else {
                close();
            }
        });

        sheet.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                // A nested dialog (the calendar) stops propagation and closes itself first.
                if (innerScope()) {
                    return;
                }

                event.preventDefault();
                close();
                return;
            }

            if (event.key !== 'Tab') {
                return;
            }

            var list = focusables(innerScope());
            if (list.length === 0) {
                return;
            }

            var first = list[0];
            var last = list[list.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        });

        return controller;
    }

    var addForm = document.getElementById('bonAddForm');
    var addBarangId = document.getElementById('bonAddBarangId');
    var addSearch = document.getElementById('bonAddSearch');
    var addSheet = document.getElementById('bonAddSheet');
    var addList = addSheet ? addSheet.querySelector('.bon-add-list') : null;

    // The sheet asks the server for its rows, so the page carries only the first page of stock.
    var addSearchUrl = addSheet ? addSheet.getAttribute('data-search-url') : null;
    var debounceMs = 250;
    var addTimer = null;
    var addPending = null;

    // One tap is one line; a second tap before the POST lands would write the line twice.
    var addSubmitting = false;

    function loadAddPage(term, page, append) {
        if (!addList || !addSearchUrl) {
            return;
        }

        if (addPending) {
            // Drop the in-flight response: a newer keystroke already asked for the latest term.
            addPending.abort();
        }

        var request = $.ajax({
            url: addSearchUrl,
            method: 'GET',
            data: { q: term, page: page },
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        });

        addPending = request;

        request
            .done(function (html) {
                if (append) {
                    appendAddPage(html);
                    return;
                }

                // Only the rows are replaced, so the box keeps focus and its text.
                addList.innerHTML = html;
            })
            .fail(function (xhr, status) {
                if (status !== 'abort') {
                    addList.innerHTML =
                        '<p class="bon-customer-empty">Gagal memuat barang. Coba lagi.</p>';
                }
            })
            .always(function () {
                if (addPending === request) {
                    addPending = null;
                }
            });
    }

    function appendAddPage(html) {
        // Parsed detached, so the wrapper the response carries cannot duplicate the rows already on
        // the sheet; only the new rows and the next button are moved in.
        var response = $('<div>').html(html);
        var incoming = response.find('.bon-barang-rows').html();
        var rowsEl = addList.querySelector('.bon-barang-rows');

        if (incoming && rowsEl) {
            rowsEl.insertAdjacentHTML('beforeend', incoming);
        }

        var button = addList.querySelector('.bon-picker-load-more');
        var next = response.find('.bon-picker-load-more');

        if (!button) {
            return;
        }

        // No button in the response means the server has no page after this one.
        if (next.length) {
            button.replaceWith(next[0]);
        } else {
            button.remove();
        }
    }

    function resetAddSearch() {
        if (!addSearch || !addList) {
            return;
        }

        var hadTerm = addSearch.value.trim() !== '';
        addSearch.value = '';

        // The sheet always reopens on the first page, never on the last search it fetched.
        if (hadTerm) {
            loadAddPage('', 1, false);
        }
    }

    var calendar = window.BonCalendar
        ? window.BonCalendar.create({
            overlayId: 'bonPayCalendar',
            triggerId: 'bonPayTanggalBtn',
            valueId: 'bonPayTanggalValue',
            inputId: 'bonPayTanggal',
            titleId: 'bonCalTitle',
            gridId: 'bonCalGrid'
        })
        : null;

    createSheet({
        sheetId: 'bonPaySheet',
        triggerId: 'catatPembayaranBtn',
        panelSelector: '.bon-pay-sheet',
        initialFocusId: 'bonPayJumlah',
        closeSelector: '[data-bon-pay-close], [data-bon-pay-save]',
        getFocusScope: function () {
            return calendar && calendar.isOpen() ? calendar.popup : null;
        },
        onClose: function () {
            if (calendar) {
                calendar.close(false);
            }
        }
    });

    createSheet({
        sheetId: 'bonAddSheet',
        triggerId: 'tambahBarangBtn',
        panelSelector: '.bon-add-sheet',
        initialFocusId: 'bonAddSearch',
        closeSelector: '[data-bon-add-close]',
        pickSelector: '.bon-picker-item',
        onPick: function (item) {
            // One tap writes one unit; the server copies the name, unit, and price from the stock list.
            if (addSubmitting || !addForm || !addBarangId) {
                return;
            }

            var id = item.getAttribute('data-id') || '';
            if (id === '') {
                return;
            }

            addSubmitting = true;
            addBarangId.value = id;
            addForm.submit();
        },
        onClose: resetAddSearch
    });

    // Typing asks the server for the first page that matches; the box never filters a stale page.
    if (addSearch && addSearchUrl) {
        addSearch.addEventListener('input', function () {
            window.clearTimeout(addTimer);
            addTimer = window.setTimeout(function () {
                loadAddPage(addSearch.value.trim(), 1, false);
            }, debounceMs);
        });
    }

    if (addList) {
        // Delegated on the list, so the handler survives every search re-render.
        addList.addEventListener('click', function (event) {
            var button = event.target.closest('.bon-picker-load-more');
            if (!button) {
                return;
            }

            // The server owns the page number: every render ships a button pointing at the page after it.
            var nextPage = parseInt(button.getAttribute('data-next-page'), 10);
            if (!nextPage) {
                return;
            }

            loadAddPage(addSearch ? addSearch.value.trim() : '', nextPage, true);
        });
    }
});

