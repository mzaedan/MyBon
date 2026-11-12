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
})();