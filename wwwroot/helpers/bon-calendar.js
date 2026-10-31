/* Shared Bon calendar popup: the Tanggal row from the Catat pembayaran sheet. A trigger button
   opens the popup, and the day it picks is written into a hidden ISO field. Owner scripts pass the
   ids they painted; the page hands the overlay to the shell when it would otherwise scroll away
   with the form. No library: the grid is built with the DOM API. */
(function (global) {
    'use strict';

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

    function create(options) {
        var shell = document.querySelector('.app-shell');
        var overlay = document.getElementById(options.overlayId);
        var trigger = document.getElementById(options.triggerId);
        var valueEl = document.getElementById(options.valueId);
        var input = document.getElementById(options.inputId);
        var popup = overlay ? overlay.querySelector('.bon-cal-popup') : null;
        var title = document.getElementById(options.titleId);
        var grid = document.getElementById(options.gridId);

        if (!shell || !overlay || !trigger || !valueEl || !input || !popup || !title || !grid) {
            return null;
        }

        // Overlays are absolute, so one painted inside the scrolling main has to live on the shell.
        // Inside a bottom sheet no host is needed: the overlay already travels with the sheet.
        if (options.host) {
            options.host.appendChild(overlay);
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
            if (event.key === 'Escape') {
                // Close the popup only; a parent sheet keeps its own Escape.
                event.preventDefault();
                event.stopPropagation();
                close();
                return;
            }

            if (event.key !== 'Tab') {
                return;
            }

            // Without a sheet around it the popup traps its own focus, like the pickers do.
            var list = Array.prototype.slice.call(
                overlay.querySelectorAll('button:not([disabled]), [href]')
            );
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

        renderTrigger();

        return {
            close: close,
            isOpen: function () {
                return !overlay.hidden;
            },
            popup: popup
        };
    }

    global.BonCalendar = { create: create };
})(window);

