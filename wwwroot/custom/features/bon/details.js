/* Bon > Details: the Catat pembayaran sheet with its Tanggal calendar (drawn by the shared
   helpers/bon-calendar.js), the Tambah barang sheet with the per-row quantity stepper, and the stock
   rows that sheet lists. Saving the pay sheet posts #bonPayForm, and picking a barang submits
   #bonAddForm with the count its stepper shows; both redirect back, so this page repaints from the
   notebook. */
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

        var controller = { close: close, open: open };

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
    var addJumlah = document.getElementById('bonAddJumlah');
    var addSearch = document.getElementById('bonAddSearch');
    var addSheet = document.getElementById('bonAddSheet');
    var addList = addSheet ? addSheet.querySelector('.bon-add-list') : null;

    // The sheet asks the server for its rows, so the page carries only the first page of stock.
    var addSearchUrl = addSheet ? addSheet.getAttribute('data-search-url') : null;
    var debounceMs = 250;
    var addTimer = null;
    var addPending = null;

    // The stepper's range matches AddBonItemRequest, so a pick never writes zero or a rejected count.
    var minJumlah = 1;
    var maxJumlah = 9999;

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

    function readStepper(row) {
        var valueEl = row ? row.querySelector('[data-bon-step-value]') : null;
        var value = valueEl ? parseInt(valueEl.textContent, 10) : minJumlah;

        if (isNaN(value)) {
            return minJumlah;
        }

        return Math.min(Math.max(value, minJumlah), maxJumlah);
    }

    function stepStepper(button) {
        var row = button.closest('.bon-add-item');
        var valueEl = row ? row.querySelector('[data-bon-step-value]') : null;
        if (!valueEl) {
            return;
        }

        var step = parseInt(button.getAttribute('data-bon-step'), 10) || 0;
        var next = readStepper(row) + step;

        // Both buttons stay enabled; the count simply stops at one and at the request's top.
        valueEl.textContent = String(Math.min(Math.max(next, minJumlah), maxJumlah));
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

    var paySheetEl = document.getElementById('bonPaySheet');
    var paySheet = createSheet({
        sheetId: 'bonPaySheet',
        triggerId: 'catatPembayaranBtn',
        panelSelector: '.bon-pay-sheet',
        initialFocusId: 'bonPayJumlah',
        // The save button is the form's submit, so only the close controls dismiss the sheet.
        closeSelector: '[data-bon-pay-close]',
        getFocusScope: function () {
            return calendar && calendar.isOpen() ? calendar.popup : null;
        },
        onClose: function () {
            if (calendar) {
                calendar.close(false);
            }
        }
    });

    var payForm = document.getElementById('bonPayForm');
    var payJumlah = document.getElementById('bonPayJumlah');
    var payJumlahError = document.getElementById('bonPayJumlahError');
    var payTanggal = document.getElementById('bonPayTanggal');
    var payTanggalError = document.getElementById('bonPayTanggalError');
    var payTanggalBtn = document.getElementById('bonPayTanggalBtn');
    var paySubmitting = false;

    // The amount reads the way the server does: dots group thousands, a comma marks the decimals.
    function readPayJumlah() {
        var digits = String(payJumlah ? payJumlah.value : '')
            .replace(/\./g, '')
            .replace(/\s/g, '')
            .replace(',', '.');
        var amount = parseFloat(digits);
        return isNaN(amount) ? 0 : amount;
    }

    function setPayFieldError(field, errorEl, message) {
        if (errorEl) {
            errorEl.textContent = message;
            errorEl.hidden = false;
        }

        if (field) {
            field.classList.add('input-validation-error');
            field.setAttribute('aria-invalid', 'true');
        }
    }

    function clearPayFieldError(field, errorEl) {
        if (!errorEl || errorEl.hidden) {
            return;
        }

        errorEl.textContent = '';
        errorEl.hidden = true;

        if (field) {
            field.classList.remove('input-validation-error');
            field.removeAttribute('aria-invalid');
        }
    }

    if (payForm) {
        payForm.addEventListener('submit', function (event) {
            // An empty or zero amount is answered under the field, so the sheet never posts it and the
            // page banner above the title stays out of it.
            if (readPayJumlah() <= 0) {
                event.preventDefault();
                setPayFieldError(payJumlah, payJumlahError, 'Jumlah bayar wajib diisi');

                if (payJumlah) {
                    payJumlah.focus();
                }

                return;
            }

            // Saving posts the sheet to the server; the redirect repaints the page, so only a second click
            // before that POST lands needs stopping.
            if (paySubmitting) {
                event.preventDefault();
                return;
            }

            paySubmitting = true;
        });
    }

    if (payJumlah) {
        // The message belongs to the field: it goes the moment the field holds a real amount.
        payJumlah.addEventListener('input', function () {
            if (readPayJumlah() > 0) {
                clearPayFieldError(payJumlah, payJumlahError);
            }
        });
    }

    // The calendar writes the hidden Tanggal field itself, so its message clears on the overlay's clicks,
    // after the calendar's own handler has stored the day it picked.
    var payCalendarOverlay = document.getElementById('bonPayCalendar');
    if (payCalendarOverlay) {
        payCalendarOverlay.addEventListener('click', function () {
            if (payTanggal && payTanggal.value !== '') {
                clearPayFieldError(payTanggalBtn, payTanggalError);
            }
        });
    }

    // A rejected save repaints the page, so the sheet reopens on the message the server left for it.
    if (paySheet && paySheetEl && paySheetEl.getAttribute('data-error') === 'true') {
        paySheet.open();

        if (payJumlah) {
            payJumlah.focus();
        }
    }

    createSheet({
        sheetId: 'bonAddSheet',
        triggerId: 'tambahBarangBtn',
        panelSelector: '.bon-add-sheet',
        initialFocusId: 'bonAddSearch',
        closeSelector: '[data-bon-add-close]',
        pickSelector: '.bon-picker-item',
        onPick: function (item) {
            // One tap writes the count the row's stepper shows; the server copies the name, unit, and
            // price from the stock list.
            if (addSubmitting || !addForm || !addBarangId || !addJumlah) {
                return;
            }

            var id = item.getAttribute('data-id') || '';
            if (id === '') {
                return;
            }

            addSubmitting = true;
            addBarangId.value = id;
            addJumlah.value = String(readStepper(item.closest('.bon-add-item')));
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
            // The stepper sits beside the pick button, so it never adds a line: it only sets the count.
            var step = event.target.closest('[data-bon-step]');
            if (step) {
                stepStepper(step);
                return;
            }

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

