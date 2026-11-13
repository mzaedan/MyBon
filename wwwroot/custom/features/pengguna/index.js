(function () {
    'use strict';

    // Enabling or disabling an account is one tap away from the list, so the button asks first.
    var forms = document.querySelectorAll('form[data-confirm]');

    Array.prototype.forEach.call(forms, function (form) {
        form.addEventListener('submit', function (event) {
            var message = form.getAttribute('data-confirm');
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    // The reset form replaces the action row while it is open; Batal closes it without posting.
    var resets = document.querySelectorAll('.pengguna-reset');

    Array.prototype.forEach.call(resets, function (details) {
        var cancel = details.querySelector('[data-pengguna-reset-cancel]');
        var field = details.querySelector('input[type="password"]');

        // Opening puts the caret in the field the user came for.
        details.addEventListener('toggle', function () {
            if (details.open && field) {
                field.focus();
            }
        });

        if (cancel) {
            cancel.addEventListener('click', function () {
                details.open = false;

                var summary = details.querySelector('summary');
                if (summary) {
                    summary.focus();
                }
            });
        }
    });
})();