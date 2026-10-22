/* Bon > Create: the barang lines and the two bottom sheets. Both pickers write into the
   form fields; saving the bon itself waits for the Bon dinamis plan. */
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    var shell = document.querySelector('.app-shell');
    if (!shell) {
        return;
    }

    initBarangLines(shell);
    initCustomerPicker(shell);

    function initBarangLines(shell) {
        var picker = document.getElementById('bonPicker');
        var container = document.getElementById('bonLinesContainer');
        var template = document.getElementById('bonLineTemplate');
        var tambahBtn = document.getElementById('tambahBarangBtn');
        var totalEl = document.getElementById('totalBelanja');

        if (!picker || !container || !template || !tambahBtn) {
            return;
        }

        // The overlays are absolute, so they live on the shell instead of inside the scrolling main.
        shell.appendChild(picker);

        var sheet = picker.querySelector('.bon-picker-sheet');
        var lastFocus = null;
        var currentLine = null;
        var lineCounter = 0;

        function focusables() {
            return Array.prototype.slice.call(
                sheet.querySelectorAll('button:not([disabled])')
            );
        }

        function openPicker(triggerBtn) {
            currentLine = triggerBtn.closest('.bon-line');
            lastFocus = document.activeElement;
            picker.hidden = false;
            picker.classList.add('is-open');
            triggerBtn.setAttribute('aria-expanded', 'true');
            var first = focusables()[0];
            if (first) {
                first.focus();
            }
        }

        function closePicker() {
            picker.hidden = true;
            picker.classList.remove('is-open');
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

            var nama = item.getAttribute('data-nama') || '';
            var harga = item.getAttribute('data-harga') || 'Rp 0';
            var satuan = item.getAttribute('data-satuan') || '-';

            var hidden = currentLine.querySelector('.barang-hidden');
            var trigger = currentLine.querySelector('.barang-trigger');
            var label = currentLine.querySelector('.barang-trigger-label');
            var hargaEl = currentLine.querySelector('.barang-harga');
            var satuanEl = currentLine.querySelector('.barang-satuan');

            if (hidden) {
                hidden.value = nama;
            }
            if (label) {
                label.textContent = nama;
            }
            if (trigger) {
                trigger.classList.remove('is-empty');
            }
            if (hargaEl) {
                hargaEl.textContent = harga;
            }
            if (satuanEl) {
                satuanEl.textContent = satuan;
            }

            hitungSubtotal(currentLine);
            hitungTotalBelanja();
            closePicker();
        }

        function hitungSubtotal(line) {
            var hargaText = (line.querySelector('.barang-harga') || {}).textContent || 'Rp 0';
            var jumlah = parseInt((line.querySelector('.barang-jumlah') || {}).value, 10) || 1;
            var angka = parseInt(hargaText.replace(/[^0-9]/g, ''), 10) || 0;
            var subtotal = angka * jumlah;
            var subEl = line.querySelector('.barang-subtotal');
            if (subEl) {
                subEl.textContent = 'Rp ' + subtotal.toLocaleString('id-ID');
            }
        }

        function hitungTotalBelanja() {
            var lines = container.querySelectorAll('.bon-line');
            var total = 0;
            lines.forEach(function (line) {
                var subText = (line.querySelector('.barang-subtotal') || {}).textContent || 'Rp 0';
                total += parseInt(subText.replace(/[^0-9]/g, ''), 10) || 0;
            });
            totalEl.textContent = 'Rp ' + total.toLocaleString('id-ID');
        }

        function buatBaris() {
            var index = lineCounter++;
            var clone = template.content.cloneNode(true);
            var lineDiv = clone.querySelector('.bon-line');
            lineDiv.setAttribute('data-line-index', index);

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
                hitungTotalBelanja();
            });

            container.appendChild(clone);
            return lineDiv;
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

        buatBaris();

        tambahBtn.addEventListener('click', buatBaris);

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

        // Same overlay container as the barang picker, so the scrim covers the whole shell.
        shell.appendChild(picker);

        var sheet = picker.querySelector('.bon-picker-sheet');
        var value = trigger.querySelector('.bon-customer-trigger-label');
        var rows = Array.prototype.slice.call(picker.querySelectorAll('.bon-customer-row'));
        var lastFocus = null;

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
                empty.hidden = true;
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
            var nama = item.getAttribute('data-nama') || '';
            field.value = nama;

            if (value) {
                value.textContent = nama;
            }

            trigger.classList.remove('is-empty');
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

        // The export paints a search row; the sheet filters its four rows by name so the box is not dead.
        search.addEventListener('input', function () {
            var term = search.value.trim().toLowerCase();
            var matches = 0;

            rows.forEach(function (row) {
                var nameEl = row.querySelector('.bon-customer-item-name');
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
});
