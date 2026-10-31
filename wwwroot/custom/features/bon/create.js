/* Bon > Create: the customer and item sheets, the Tanggal calendar shared with
   helpers/bon-calendar.js, the barang lines, and the running totals. The form posts one
   CreateBonRequest: PelangganId, Tanggal, TotalBayar, and Items[i].BarangId / Items[i].Jumlah.
   Prices are shown only; the server re-reads them from the stock list when the bon is saved. The item
   sheet asks the server for its rows, so the page never carries the whole stock list. */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var shell = document.querySelector('.app-shell');
    if (!shell) {
        return;
    }

    initBarangLines(shell);
    initCustomerPicker(shell);
    initTanggalCalendar(shell);

    // The Tanggal row opens the same calendar popup the Catat pembayaran sheet uses.
    function initTanggalCalendar(shell) {
        if (!window.BonCalendar) {
            return;
        }

        window.BonCalendar.create({
            // The calendar is absolute, so it lives on the shell instead of inside the scrolling form.
            host: shell,
            overlayId: 'bonTanggalCalendar',
            triggerId: 'tanggalBtn',
            valueId: 'tanggalValue',
            inputId: 'tanggal',
            titleId: 'bonTanggalCalTitle',
            gridId: 'bonTanggalCalGrid'
        });
    }

    // Amounts read like the notebook: Rp 78.000, with at most two decimals.
    function formatRupiah(value) {
        return 'Rp ' + (Number(value) || 0).toLocaleString('id-ID', { maximumFractionDigits: 2 });
    }

    // Both sheets filter the rows they already carry; the box never asks the server again.
    function wireSearch(input, rows, empty, nameSelector) {
        if (!input) {
            return;
        }

        input.addEventListener('input', function () {
            var term = input.value.trim().toLowerCase();
            var matches = 0;

            rows.forEach(function (row) {
                var nameEl = row.querySelector(nameSelector);
                var name = nameEl ? nameEl.textContent.toLowerCase() : '';
                var isMatch = term === '' || name.indexOf(term) !== -1;
                row.hidden = !isMatch;
                if (isMatch) {
                    matches++;
                }
            });

            if (empty) {
                empty.hidden = matches > 0;
            }
        });
    }

    function initBarangLines(shell) {
        var picker = document.getElementById('bonPicker');
        var container = document.getElementById('bonLinesContainer');
        var template = document.getElementById('bonLineTemplate');
        var tambahBtn = document.getElementById('tambahBarangBtn');
        var totalEl = document.getElementById('totalBelanja');

        if (!picker || !container || !template || !tambahBtn || !totalEl) {
            return;
        }

        // The overlays are absolute, so they live on the shell instead of inside the scrolling main.
        shell.appendChild(picker);

        // The item sheet asks the server for its rows, so the page carries only the first page.
        var sheet = picker.querySelector('.bon-picker-sheet');
        var listEl = picker.querySelector('.bon-picker-list');
        var search = document.getElementById('bonBarangSearch');
        var searchUrl = picker.getAttribute('data-search-url');
        var debounceMs = 250;
        var timer = null;
        var pending = null;
        var lastFocus = null;
        var currentLine = null;

        wireBarangSearch();

        function focusables() {
            return Array.prototype.slice.call(
                sheet.querySelectorAll('button:not([disabled]), input:not([disabled]), [href]')
            );
        }

        // Without the endpoint the sheet keeps the rows the page shipped and filters them here.
        function wireBarangSearch() {
            if (!search || !listEl) {
                return;
            }

            if (!searchUrl) {
                var rows = Array.prototype.slice.call(picker.querySelectorAll('.bon-barang-row'));
                wireSearch(search, rows, listEl.querySelector('.bon-customer-empty'), '.bon-picker-item-name');
                return;
            }

            search.addEventListener('input', function () {
                window.clearTimeout(timer);
                timer = window.setTimeout(function () {
                    loadPage(search.value.trim(), 1, false);
                }, debounceMs);
            });
        }

        function loadPage(term, page, append) {
            if (!listEl || !searchUrl) {
                return;
            }

            if (pending) {
                // Drop the in-flight response: a newer keystroke already asked for the latest term.
                pending.abort();
            }

            var request = $.ajax({
                url: searchUrl,
                method: 'GET',
                data: { q: term, page: page },
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            pending = request;

            request
                .done(function (html) {
                    if (append) {
                        appendPage(html);
                        return;
                    }

                    // Only the rows are replaced, so the box keeps focus and its text.
                    listEl.innerHTML = html;
                })
                .fail(function (xhr, status) {
                    if (status !== 'abort') {
                        listEl.innerHTML =
                            '<p class="bon-customer-empty">Gagal memuat barang. Coba lagi.</p>';
                    }
                })
                .always(function () {
                    if (pending === request) {
                        pending = null;
                    }
                });
        }

        function appendPage(html) {
            // Parsed detached, so the wrapper the response carries cannot duplicate the rows already
            // on the sheet; only the new rows and the next button are moved in.
            var response = $('<div>').html(html);
            var incoming = response.find('.bon-barang-rows').html();
            var rowsEl = listEl.querySelector('.bon-barang-rows');

            if (incoming && rowsEl) {
                rowsEl.insertAdjacentHTML('beforeend', incoming);
            }

            var button = listEl.querySelector('.bon-picker-load-more');
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

        function resetSearch() {
            if (!search) {
                return;
            }

            var hadTerm = search.value.trim() !== '';
            search.value = '';

            // The sheet always reopens on the first page, never on the last search it fetched.
            if (hadTerm) {
                loadPage('', 1, false);
            }
        }

        function openPicker(triggerBtn) {
            currentLine = triggerBtn.closest('.bon-line');
            lastFocus = document.activeElement;
            picker.hidden = false;
            picker.classList.add('is-open');
            triggerBtn.setAttribute('aria-expanded', 'true');

            if (search) {
                search.focus();
                return;
            }

            var first = focusables()[0];
            if (first) {
                first.focus();
            }
        }

        function closePicker() {
            picker.hidden = true;
            picker.classList.remove('is-open');

            resetSearch();

            if (lastFocus) {
                var trigger = lastFocus.closest('.bon-picker-trigger');
                if (trigger) {
                    trigger.setAttribute('aria-expanded', 'false');
                }
                lastFocus.focus();
            }
        }

        function pick(item) {
            if (!currentLine) {
                return;
            }

            var id = item.getAttribute('data-id') || '';
            var nama = item.getAttribute('data-nama') || '';
            var satuan = item.getAttribute('data-satuan') || '-';
            var harga = parseFloat(item.getAttribute('data-harga')) || 0;

            var hidden = currentLine.querySelector('.barang-hidden');
            var trigger = currentLine.querySelector('.barang-trigger');
            var label = currentLine.querySelector('.barang-trigger-label');
            var hargaEl = currentLine.querySelector('.barang-harga');
            var satuanEl = currentLine.querySelector('.barang-satuan');

            // The form posts the item id; the name, unit, and price are painted only.
            if (hidden) {
                hidden.value = id;
            }
            if (label) {
                label.textContent = nama;
            }
            if (trigger) {
                trigger.classList.remove('is-empty');
            }
            if (satuanEl) {
                satuanEl.textContent = satuan;
            }
            if (hargaEl) {
                hargaEl.setAttribute('data-harga', String(harga));
                hargaEl.textContent = formatRupiah(harga);
            }

            hitungSubtotal(currentLine);
            hitungTotalBelanja();
            closePicker();
        }

        function hargaBaris(line) {
            var el = line.querySelector('.barang-harga');
            return el ? parseFloat(el.getAttribute('data-harga')) || 0 : 0;
        }

        function jumlahBaris(line) {
            var el = line.querySelector('.barang-jumlah');
            var value = el ? parseInt(el.value, 10) : 0;
            return isNaN(value) ? 0 : value;
        }

        function hitungSubtotal(line) {
            var subEl = line.querySelector('.barang-subtotal');
            if (subEl) {
                subEl.textContent = formatRupiah(hargaBaris(line) * jumlahBaris(line));
            }
        }

        function hitungTotalBelanja() {
            var total = 0;
            container.querySelectorAll('.bon-line').forEach(function (line) {
                total += hargaBaris(line) * jumlahBaris(line);
            });
            totalEl.textContent = formatRupiah(total);
        }

        // Model binding reads these names. The collection binder only walks Items[0], Items[1], ...
        // and stops at the first gap, so every add, remove, and submit renumbers the lines in order.
        function renumberLines() {
            var index = 0;

            container.querySelectorAll('.bon-line').forEach(function (line) {
                var hidden = line.querySelector('.barang-hidden');
                var jumlah = line.querySelector('.barang-jumlah');

                if (hidden) {
                    hidden.name = 'Items[' + index + '].BarangId';
                }
                if (jumlah) {
                    jumlah.name = 'Items[' + index + '].Jumlah';
                }

                line.setAttribute('data-line-index', index);
                index++;
            });
        }

        function buatBaris(data) {
            var clone = template.content.cloneNode(true);
            var lineDiv = clone.querySelector('.bon-line');

            var trigger = lineDiv.querySelector('.barang-trigger');
            trigger.addEventListener('click', function () {
                openPicker(trigger);
            });

            var jumlahInput = lineDiv.querySelector('.barang-jumlah');
            jumlahInput.addEventListener('input', function () {
                hitungSubtotal(lineDiv);
                hitungTotalBelanja();
            });

            var hapusBtn = lineDiv.querySelector('.hapus-barang');
            hapusBtn.addEventListener('click', function () {
                if (container.querySelectorAll('.bon-line').length <= 1) {
                    resetBaris(lineDiv);
                    return;
                }
                lineDiv.remove();
                renumberLines();
                hitungTotalBelanja();
            });

            container.appendChild(clone);
            renumberLines();

            // A line a rejected POST handed back is painted exactly like one picked by hand.
            if (data) {
                isiBaris(lineDiv, data);
            }
        }

        function isiBaris(line, data) {
            var hidden = line.querySelector('.barang-hidden');
            var trigger = line.querySelector('.barang-trigger');
            var label = line.querySelector('.barang-trigger-label');
            var hargaEl = line.querySelector('.barang-harga');
            var satuanEl = line.querySelector('.barang-satuan');
            var jumlah = line.querySelector('.barang-jumlah');
            var harga = Number(data.harga) || 0;

            // The form posts the item id; the name, unit, and price are painted only.
            if (hidden) {
                hidden.value = data.barangId != null ? String(data.barangId) : '';
            }
            if (label) {
                label.textContent = data.nama || 'Pilih barang';
            }
            if (trigger) {
                trigger.classList.toggle('is-empty', !data.barangId);
            }
            if (hargaEl) {
                hargaEl.setAttribute('data-harga', String(harga));
                hargaEl.textContent = formatRupiah(harga);
            }
            if (satuanEl) {
                satuanEl.textContent = data.satuan || '-';
            }
            if (jumlah) {
                jumlah.value = data.jumlah || 1;
            }

            hitungSubtotal(line);
            hitungTotalBelanja();
        }

        function resetBaris(line) {
            var hidden = line.querySelector('.barang-hidden');
            var trigger = line.querySelector('.barang-trigger');
            var label = line.querySelector('.barang-trigger-label');
            var hargaEl = line.querySelector('.barang-harga');
            var satuanEl = line.querySelector('.barang-satuan');
            var jumlah = line.querySelector('.barang-jumlah');
            var subEl = line.querySelector('.barang-subtotal');

            if (hidden) {
                hidden.value = '';
            }
            if (label) {
                label.textContent = 'Pilih barang';
            }
            if (trigger) {
                trigger.classList.add('is-empty');
            }
            if (hargaEl) {
                hargaEl.setAttribute('data-harga', '0');
                hargaEl.textContent = 'Rp 0';
            }
            if (satuanEl) {
                satuanEl.textContent = '-';
            }
            if (jumlah) {
                jumlah.value = '1';
            }
            if (subEl) {
                subEl.textContent = 'Rp 0';
            }

            hitungTotalBelanja();
        }

        // A rejected POST hands its lines back as JSON, so every picked item is painted again instead
        // of the form starting over.
        function bacaBarisAwal() {
            var el = document.getElementById('bonInitialLines');
            if (!el) {
                return [];
            }

            try {
                var parsed = JSON.parse(el.textContent || '[]');
                return Array.isArray(parsed) ? parsed : [];
            } catch (error) {
                // A malformed payload is treated as "no lines", so the form still opens.
                return [];
            }
        }

        var initialLines = bacaBarisAwal();

        if (initialLines.length === 0) {
            buatBaris();
        } else {
            initialLines.forEach(function (line) {
                buatBaris(line);
            });
        }

        tambahBtn.addEventListener('click', function () {
            buatBaris();
        });

        if (listEl) {
            // Delegated on the list, so the handler survives every search re-render.
            listEl.addEventListener('click', function (event) {
                var button = event.target.closest('.bon-picker-load-more');
                if (!button) {
                    return;
                }

                // The server owns the page number: every render ships a button pointing at the page after it.
                var nextPage = parseInt(button.getAttribute('data-next-page'), 10);
                if (!nextPage) {
                    return;
                }

                loadPage(search ? search.value.trim() : '', nextPage, true);
            });
        }

        picker.addEventListener('click', function (event) {
            if (event.target.closest('[data-bon-picker-close]')) {
                closePicker();
                return;
            }

            var item = event.target.closest('.bon-picker-item');
            if (item) {
                pick(item);
            }
        });

        picker.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                event.preventDefault();
                closePicker();
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

        var form = container.closest('form');
        if (form) {
            form.addEventListener('submit', function () {
                // A line nobody filled carries no item. Dropping it here keeps the server from rejecting
                // a row the shopkeeper never meant to write.
                container.querySelectorAll('.bon-line').forEach(function (line) {
                    var hidden = line.querySelector('.barang-hidden');
                    if (!hidden || hidden.value === '') {
                        line.remove();
                    }
                });

                renumberLines();
                hitungTotalBelanja();
            });
        }
    }

    function initCustomerPicker(shell) {
        var picker = document.getElementById('bonCustomerPicker');
        var trigger = document.getElementById('pilihPelangganBtn');
        var search = document.getElementById('bonCustomerSearch');
        var empty = document.getElementById('bonCustomerEmpty');
        var field = document.getElementById('pelanggan');

        if (!picker || !trigger || !search || !field) {
            return;
        }

        // Same overlay container as the item picker, so the scrim covers the whole shell.
        shell.appendChild(picker);

        var sheet = picker.querySelector('.bon-picker-sheet');
        var value = trigger.querySelector('.bon-customer-trigger-label');
        var rows = Array.prototype.slice.call(picker.querySelectorAll('.bon-customer-row'));
        var lastFocus = null;

        wireSearch(search, rows, empty, '.bon-customer-item-name');

        function focusables() {
            return Array.prototype.slice.call(
                sheet.querySelectorAll('button:not([disabled]), input:not([disabled]), [href]')
            );
        }

        function resetSearch() {
            search.value = '';
            rows.forEach(function (row) {
                row.hidden = false;
            });
            if (empty) {
                empty.hidden = rows.length > 0;
            }
        }

        function open() {
            lastFocus = trigger;
            picker.hidden = false;
            picker.classList.add('is-open');
            trigger.setAttribute('aria-expanded', 'true');
            search.focus();
        }

        function close() {
            if (picker.hidden) {
                return;
            }

            picker.hidden = true;
            picker.classList.remove('is-open');
            trigger.setAttribute('aria-expanded', 'false');
            resetSearch();

            if (lastFocus) {
                lastFocus.focus();
            }
        }

        function pick(item) {
            var id = item.getAttribute('data-id') || '';
            var nama = item.getAttribute('data-nama') || '';

            // The form posts the customer id; the name is painted only.
            field.value = id;
            if (value) {
                value.textContent = nama || 'Pilih pelanggan';
            }
            trigger.classList.toggle('is-empty', id === '');
            close();
        }

        trigger.addEventListener('click', open);

        picker.addEventListener('click', function (event) {
            if (event.target.closest('[data-bon-customer-close]')) {
                close();
                return;
            }

            var item = event.target.closest('.bon-customer-item');
            if (item) {
                pick(item);
            }
        });

        picker.addEventListener('keydown', function (event) {
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
    }
});
