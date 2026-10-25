/* Bon > Details: the Catat pembayaran sheet with its Tanggal calendar, the Tambah barang sheet,
   and the local item filter. Visual only: Simpan closes the payment sheet and picking a barang
   closes the picker until both get their own POST actions from the Bon dinamis plan. */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var shell = document.querySelector('.app-shell');
    if (!shell) {
        return;
    }

    var MONTHS = [
        'Januari', 'Februari', 'Maret', 'April', 'Mei', 'Juni',
        'Juli', 'Agustus', 'September', 'Oktober', 'November', 'Desember'
    ];

    function pad2(value) {
        return value < 10 ? '0' + value : String(value);
    }

    function parseIso(value) {
        var match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
        if (!match) {
            return null;
        }

        return new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    }

    function toIso(date) {
        return date.getFullYear() + '-' + pad2(date.getMonth() + 1) + '-' + pad2(date.getDate());
    }

    function startOfMonth(date) {
        return new Date(date.getFullYear(), date.getMonth(), 1);
    }

    function addDays(date, days) {
        return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
    }

    function addMonths(date, months) {
        return new Date(date.getFullYear(), date.getMonth() + months, 1);
    }

    function sameDay(a, b) {
        return a.getFullYear() === b.getFullYear()
            && a.getMonth() === b.getMonth()
            && a.getDate() === b.getDate();
    }

    function formatTanggal(date) {
        return date.getDate() + ' ' + MONTHS[date.getMonth()] + ' ' + date.getFullYear();
    }

    var openSheet = null;

    function createCalendar() {
        var overlay = document.getElementById('bonPayCalendar');
        var trigger = document.getElementById('bonPayTanggalBtn');
        var valueEl = document.getElementById('bonPayTanggalValue');
        var input = document.getElementById('bonPayTanggal');
        var popup = overlay ? overlay.querySelector('.bon-cal-popup') : null;
        var title = document.getElementById('bonCalTitle');
        var grid = document.getElementById('bonCalGrid');

        if (!overlay || !trigger || !valueEl || !input || !popup || !title || !grid) {
            return null;
        }

        var selected = parseIso(input.value);
        var view = startOfMonth(selected || new Date());

        function renderTrigger() {
            if (selected) {
                valueEl.textContent = formatTanggal(selected);
                input.value = toIso(selected);
                trigger.classList.remove('is-empty');
                return;
            }

            valueEl.textContent = 'Pilih tanggal';
            input.value = '';
            trigger.classList.add('is-empty');
        }

        function createDay(date, today) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'bon-cal-day';
            button.setAttribute('role', 'gridcell');
            button.setAttribute('aria-label', formatTanggal(date));
            button.setAttribute('aria-selected', selected && sameDay(date, selected) ? 'true' : 'false');
            button.dataset.date = toIso(date);
            button.textContent = String(date.getDate());

            if (date.getMonth() !== view.getMonth()) {
                button.classList.add('is-outside');
            }

            if (sameDay(date, today)) {
                button.classList.add('is-today');
                button.setAttribute('aria-current', 'date');
            }

            if (selected && sameDay(date, selected)) {
                button.classList.add('is-selected');
            }

            return button;
        }

        function render() {
            title.textContent = MONTHS[view.getMonth()] + ' ' + view.getFullYear();
            grid.textContent = '';

            // Monday-first like the export's Sen..Min header, so Sunday is the last column.
            var offset = (view.getDay() + 6) % 7;
            var start = addDays(view, -offset);
            var today = new Date();

            for (var week = 0; week < 6; week++) {
                var row = document.createElement('div');
                row.className = 'bon-cal-week';
                row.setAttribute('role', 'row');

                for (var day = 0; day < 7; day++) {
                    row.appendChild(createDay(addDays(start, week * 7 + day), today));
                }

                grid.appendChild(row);
            }
        }

        function focusViewDate() {
            var target = grid.querySelector('.bon-cal-day.is-selected')
                || grid.querySelector('[data-date="' + toIso(view) + '"]')
                || grid.querySelector('.bon-cal-day');
            if (target) {
                target.focus();
            }
        }

        function position() {
            var shellRect = shell.getBoundingClientRect();
            var triggerRect = trigger.getBoundingClientRect();
            var height = popup.offsetHeight;
            var top = triggerRect.top - shellRect.top - height - 8;

            if (top < 12) {
                top = triggerRect.bottom - shellRect.top + 8;
            }

            var maxTop = shellRect.height - height - 12;
            if (top > maxTop) {
                top = Math.max(12, maxTop);
            }

            popup.style.top = Math.round(top) + 'px';
        }

        function open() {
            renderTrigger();
            render();
            overlay.hidden = false;
            overlay.classList.add('is-open');
            trigger.setAttribute('aria-expanded', 'true');
            position();
            focusViewDate();
            window.addEventListener('resize', position);
        }

        function close(restoreFocus) {
            if (overlay.hidden) {
                return;
            }

            overlay.hidden = true;
            overlay.classList.remove('is-open');
            trigger.setAttribute('aria-expanded', 'false');
            window.removeEventListener('resize', position);

            if (restoreFocus !== false) {
                trigger.focus();
            }
        }

        function pick(date) {
            selected = date;
            renderTrigger();
            close();
        }

        trigger.addEventListener('click', function () {
            view = startOfMonth(selected || new Date());
            open();
        });

        overlay.addEventListener('click', function (event) {
            if (event.target.closest('[data-bon-cal-close]')) {
                close();
                return;
            }

            if (event.target.closest('[data-bon-cal-prev]')) {
                view = addMonths(view, -1);
                render();
                focusViewDate();
                return;
            }

            if (event.target.closest('[data-bon-cal-next]')) {
                view = addMonths(view, 1);
                render();
                focusViewDate();
                return;
            }

            if (event.target.closest('[data-bon-cal-clear]')) {
                selected = null;
                renderTrigger();
                close();
                return;
            }

            if (event.target.closest('[data-bon-cal-today]')) {
                pick(new Date());
                return;
            }

            var day = event.target.closest('.bon-cal-day');
            if (!day) {
                return;
            }

            var date = parseIso(day.dataset.date);
            if (date) {
                pick(date);
            }
        });

        grid.addEventListener('keydown', function (event) {
            var steps = { ArrowLeft: -1, ArrowRight: 1, ArrowUp: -7, ArrowDown: 7 };
            var step = steps[event.key];
            if (!step) {
                return;
            }

            var days = Array.prototype.slice.call(grid.querySelectorAll('.bon-cal-day'));
            var index = days.indexOf(document.activeElement);
            var next = index === -1 ? null : days[index + step];
            if (!next) {
                return;
            }

            event.preventDefault();
            next.focus();
        });

        overlay.addEventListener('keydown', function (event) {
            if (event.key !== 'Escape') {
                return;
            }

            // Close the popup only; the sheet keeps its own Escape.
            event.preventDefault();
            event.stopPropagation();
            close();
        });

        renderTrigger();

        return {
            close: close,
            isOpen: function () {
                return !overlay.hidden;
            },
            popup: popup
        };
    }

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

            if (config.pickSelector && event.target.closest(config.pickSelector)) {
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

    var calendar = createCalendar();

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

