/* Keeps a money field numeric and shows thousands separators while typing: Barang > Create and Edit
   (Harga) and Bon > Details (Jumlah bayar on the Catat pembayaran sheet).
   Letters and symbols never enter the field: keys are blocked on beforeinput, and text arriving from
   paste, drop, or autofill is stripped to digits, dots, and commas. The separator is display only; on
   submit the dots are stripped from the value, so the posted amount carries no separator and the
   server parses plain digits ("1000" or "1000,5") instead of reading "1.000" as 1.
   Shared by every form that marks a field with data-money-input. */
(function () {
    'use strict';

    var INVALID_CHARACTER_PATTERN = /[^0-9.,]/;
    var NON_PRICE_CHARACTERS_PATTERN = /[^0-9.,]/g;

    function formatPrice(value) {
        var commaIndex = value.indexOf(',');
        var integerPart = commaIndex === -1 ? value : value.slice(0, commaIndex);
        var decimalPart = commaIndex === -1 ? null : value.slice(commaIndex + 1);

        integerPart = integerPart.replace(/\./g, '').replace(/^0+(?=\d)/, '');

        if (decimalPart !== null) {
            decimalPart = decimalPart.replace(/[.,]/g, '').slice(0, 2);
        }

        var grouped = integerPart.replace(/\B(?=(\d{3})+(?!\d))/g, '.');

        return decimalPart === null ? grouped : grouped + ',' + decimalPart;
    }

    function countDigits(value, end) {
        var digits = 0;
        for (var i = 0; i < end && i < value.length; i++) {
            var code = value.charCodeAt(i);
            if (code >= 48 && code <= 57) {
                digits++;
            }
        }
        return digits;
    }

    function caretForDigit(value, digitIndex) {
        var seen = 0;
        for (var i = 0; i < value.length; i++) {
            var code = value.charCodeAt(i);
            if (code >= 48 && code <= 57) {
                seen++;
                if (seen === digitIndex) {
                    return i + 1;
                }
            }
        }
        return value.length;
    }

    function normalise(input) {
        // Letters, spaces, and signs are removed rather than kept for the server to reject: the field
        // only ever holds digits, dots, and one comma, so what is on screen is what gets posted.
        var value = input.value.replace(NON_PRICE_CHARACTERS_PATTERN, '');

        var formatted = formatPrice(value);
        if (formatted === input.value) {
            return;
        }

        var supportsSelection = typeof input.selectionStart === 'number';
        var digitsBefore = supportsSelection
            ? countDigits(input.value, input.selectionStart)
            : countDigits(input.value, input.value.length);

        input.value = formatted;

        if (supportsSelection && input.setSelectionRange) {
            var newCaret = caretForDigit(formatted, digitsBefore);
            input.setSelectionRange(newCaret, newCaret);
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var inputs = document.querySelectorAll('[data-money-input]');

        for (var i = 0; i < inputs.length; i++) {
            (function (input) {
                if (input.value !== '') {
                    normalise(input);
                }

                // Typed characters are checked before they reach the field, so a letter never flashes
                // in the input. Paste, drop, and autofill skip this hook and are cleaned by the input handler.
                input.addEventListener('beforeinput', function (event) {
                    if (event.inputType === 'insertText'
                        && event.data
                        && INVALID_CHARACTER_PATTERN.test(event.data)) {
                        event.preventDefault();
                    }
                });

                input.addEventListener('input', function () {
                    normalise(input);
                });

                if (input.form) {
                    input.form.addEventListener('submit', function () {
                        input.value = input.value.replace(/\./g, '').replace(/\s/g, '');
                    });
                }
            })(inputs[i]);
        }
    });
})();
