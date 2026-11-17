(function () {
    'use strict';

    // The sign-in form posts a full page, so the only thing to guard is a double submit while the
    // browser waits. Native validation stays in charge of the empty-field case.
    var form = document.querySelector('form.auth-page');
    if (!form) {
        return;
    }

    var button = form.querySelector('.auth-btn-primary');

    // The eye reveals the typed password so the reader can check it before submitting; the
    // field type and the icon swap together and aria-pressed keeps the state announced.
    var toggle = form.querySelector('[data-password-toggle]');
    var password = toggle ? document.getElementById(toggle.getAttribute('aria-controls')) : null;

    if (toggle && password) {
        toggle.addEventListener('click', function () {
            var revealed = password.type === 'text';
            var label = revealed ? 'Tampilkan kata sandi' : 'Sembunyikan kata sandi';

            password.type = revealed ? 'password' : 'text';
            toggle.setAttribute('aria-pressed', String(!revealed));
            toggle.setAttribute('aria-label', label);
            toggle.setAttribute('title', label);

            var icon = toggle.querySelector('.bi');
            if (icon) {
                icon.classList.toggle('bi-eye', revealed);
                icon.classList.toggle('bi-eye-slash', !revealed);
            }

            password.focus();
        });
    }

    form.addEventListener('submit', function () {
        if (!form.checkValidity() || !button) {
            return;
        }

        button.disabled = true;
        button.textContent = 'Memeriksa...';
    });
})();