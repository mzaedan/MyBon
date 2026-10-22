/* Bon > Details: the Catat pembayaran and Tambah barang sheets. Visual only: Simpan closes
   the payment sheet and picking a barang closes the picker until both get their own POST
   actions from the Bon dinamis plan. */
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

        function focusables() {
            return Array.prototype.slice.call(
                panel.querySelectorAll('button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [href]')
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

            if (config.pickSelector && event.target.closest(config.pickSelector)) {
                close();
            }
        });

        sheet.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                event.preventDefault();
                close();
                return;
            }

            if (event.key !== 'Tab') {
                return;
            }

            var list = focusables();
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

    var addSearch = document.getElementById('bonAddSearch');
    var addEmpty = document.getElementById('bonAddEmpty');
    var addRows = Array.prototype.slice.call(document.querySelectorAll('.bon-add-row'));

    function resetAddSearch() {
        if (!addSearch) {
            return;
        }

        addSearch.value = '';

        addRows.forEach(function (row) {
            row.hidden = false;
        });

        if (addEmpty) {
            addEmpty.hidden = true;
        }
    }

    createSheet({
        sheetId: 'bonPaySheet',
        triggerId: 'catatPembayaranBtn',
        panelSelector: '.bon-pay-sheet',
        initialFocusId: 'bonPayJumlah',
        closeSelector: '[data-bon-pay-close], [data-bon-pay-save]'
    });

    createSheet({
        sheetId: 'bonAddSheet',
        triggerId: 'tambahBarangBtn',
        panelSelector: '.bon-add-sheet',
        initialFocusId: 'bonAddSearch',
        closeSelector: '[data-bon-add-close]',
        pickSelector: '.bon-add-item',
        onClose: resetAddSearch
    });

    // The export paints a search row; the sheet filters its eight rows locally so the box is not dead.
    if (addSearch) {
        addSearch.addEventListener('input', function () {
            var term = addSearch.value.trim().toLowerCase();
            var matches = 0;

            addRows.forEach(function (row) {
                var nameEl = row.querySelector('.bon-add-item-name');
                var name = nameEl ? nameEl.textContent.toLowerCase() : '';
                var isMatch = term === '' || name.indexOf(term) !== -1;
                row.hidden = !isMatch;
                if (isMatch) {
                    matches++;
                }
            });

            if (addEmpty) {
                addEmpty.hidden = matches > 0;
            }
        });
    }
});

