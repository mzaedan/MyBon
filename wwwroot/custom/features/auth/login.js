(function () {
    'use strict';

    // The sign-in form posts a full page, so the only thing to guard is a double submit while the
    // browser waits. Native validation stays in charge of the empty-field case.
    var form = document.querySelector('form.auth-page');
    if (!form) {
        return;
    }

    var button = form.querySelector('.auth-btn-primary');

    form.addEventListener('submit', function () {
        if (!form.checkValidity() || !button) {
            return;
        }

        button.disabled = true;
        button.textContent = 'Memeriksa...';
    });
})();