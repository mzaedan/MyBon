/* Barang > Create: shows thousands separators in the Harga field while typing.
   The separator is display only; on submit the dots are stripped from the value,
   so the posted harga carries no separator. */
(function () {
    'use strict';

    // Only digits, dots, and commas are treated as a price. Anything else is left
    // untouched so the server can still reject it with "Harga harus angka".
    var PRICE_PATTERN = /^[0-9.,]*$/;

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
        var value = input.value.replace(/\s/g, '');
        if (!PRICE_PATTERN.test(value)) {
            return;
        }

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