# Masuk (login) slicing plan

**Goal:** Slice the login screen from `CatatHutang-export.html` (screen `Masuk`) into `Views/Auth/Login.cshtml` on its own `_LayoutAuth` shell. Static UI only.

**Source:** `CatatHutang-export.html` (screen `Masuk`).
**Depends on:** the head assets in `Views/Shared/_Layout.cshtml`, tokens in `wwwroot/css/site.css`, `Controllers/BaseController.cs`.
**Status:** visual slice only. No model, service, database, POST action, cookie, or Tailwind. The auth behaviour itself stays in `docs/plans/plan-login.md`.

## Constraints

- Do not render the mockup iPhone status bar (time, signal, wifi, battery).
- Do not add Tailwind. Use Figtree and the existing tokens (`--accent`, `--muted`, `--border`, `--surface`).
- Keep the shell frame from `site.css`: 390px max width, centered card on desktop.
- Login sits outside the four-tab chrome. `_LayoutAuth` carries no bottom nav, no app header, and no wordmark bar; the view paints its own screen header.
- The app name is **MY BON**, not the mockup's `CatatHutang`.
- Buttons and the forgot control are visual only (`type="button"`). There is no POST target yet.
- Use Bootstrap Icons for the check inside the brand mark; do not copy Phosphor SVG paths from the export.
- Placeholders stay honest: `Tulis username`, `Tulis kata sandi`.

## Architecture

```
GET /Auth/Login -> AuthController.Login() -> Views/Auth/Login.cshtml (Layout = _LayoutAuth)
GET /login      -> same action (short alias, mapped in Program.cs before the default route)

_LayoutAuth.cshtml
  head                  fonts + Bootstrap + Bootstrap Icons + site.css
  body.app-body
    .app-shell
      main.auth-main     scrolls; 8px 20px 24px padding
        @RenderBody()

wwwroot/css/site.css    .auth-* block at the end, reusing .page-rule and .pelanggan-field*
```

## File map

- Create `Controllers/AuthController.cs`
- Create `Views/Shared/_LayoutAuth.cshtml`
- Create `Views/Auth/Login.cshtml`
- Modify `wwwroot/css/site.css`

## Slice 1: shell and route

- [ ] **Step 1:** `_LayoutAuth.cshtml` repeats the font, Bootstrap, Bootstrap Icons, and `site.css` links from `_Layout.cshtml`; it renders only `.app-shell > main.auth-main`. No bottom nav, no app header.
- [ ] **Step 2:** `AuthController.Login()` returns `View()`, so the default route serves `/Auth/Login`.

## Slice 2: markup, in export order

- [ ] **Step 1:** Header: a small **MY BON** line over the **Masuk** title, then a `.page-rule`.
- [ ] **Step 2:** Brand row: 64px white mark with a 1px accent border; a 32×37 blue receipt holding **Rp**, a white check, and a folded corner; tagline **Buku hutang warung**; the description.
- [ ] **Step 3:** Fields **Username** (`type="text"`, `autocomplete="username"`) and **Kata sandi** (`type="password"`), each reusing `.pelanggan-field*` and `.page-rule`.
- [ ] **Step 4:** **Lupa kata sandi?** right aligned.
- [ ] **Step 5:** Primary **Masuk ke buku hutang** and secondary **Buat akun toko baru**.

## Slice 3: styling

- [ ] **Step 1:** `.auth-main` owns the scroll and `8px 20px calc(24px + safe-area)` padding; `.auth-page` is a 24px column whose spacer grows.
- [ ] **Step 2:** Brand mark: 64px, 14px radius, accent border. Receipt: 32×37, 4px radius, 11px bold **Rp**, 10px check, white folded corner.
- [ ] **Step 3:** Buttons: 48px, 8px radius; primary accent, secondary white with a `--border` outline.
- [ ] **Step 4:** Focus ring `2px` on buttons and the forgot control; tap targets at least 44px.

## Verification

- [ ] `dotnet build` succeeds.
- [ ] `GET /Auth/Login` answers 200 and paints the brand row, both fields, both buttons, and the bottom note.
- [ ] No bottom nav, no iPhone status bar, no Tailwind.
- [ ] At 390px and below there is no horizontal overflow; the note stays reachable.
- [ ] Every control is keyboard focusable; nothing navigates or posts in this slice.

## Non-goals

Cookie authentication, the `pengguna` table, username/password validation, redirect by role, `ReturnUrl`, rate limiting, the Dashboard/Pelanggan/Barang guards, forgot-password flow, registration, Tailwind, and Phosphor icon paths. These stay in `docs/plans/plan-login.md`.

## Updates

- `docs/plans/plan-perbaikan-scrollbar-login.md` — the frame scrollbar is hidden (`scrollbar-width: none` + `.auth-main::-webkit-scrollbar`), so no bar paints on the frame edge on a short window.
- `docs/plans/plan-perbaikan-header-login.md` — supersedes Slice 2 Step 1 and the `8px` top padding in Slice 3 Step 1: the header now reuses the shell's `.app-wordmark` / `.app-screen-title` at the shell header's offset, and `.auth-shop` / `.auth-title` are gone.
- Sign-in now asks for **Username**, not an email: Slice 2 Steps 3 and 6 paint `login-username` (`name="Username"`, `type="text"`, `autocomplete="username"`, `autocapitalize="none"`, `spellcheck="false"`) and the bottom note reads **Lupa kata sandi atau username?**. The account key is `pengguna.username` in `docs/plans/plan-login.md`, so the field matches the credential the app will actually verify.
- The **Kata sandi** field carries an eye toggle: `login.js` swaps the input between `password` and `text`, flips `bi-eye` / `bi-eye-slash`, and keeps `aria-pressed` + `aria-label` on the 44px `.auth-password-toggle` so the state is announced. The toggle hangs over the field's 12px padding with a negative block margin, so the row keeps the username row's height.
- `docs/plans/plan-perbaikan-responsive-login.md` — `.app-shell` drops its 390px cap below 768px (`max-width: none`), so the white shell fills a 427px Pixel 9 Pro / 430px iPhone Pro Max instead of showing grey gutters; the framed card is now desktop-only, as this plan's Constraints line intended. `.auth-page` gains a 480px column cap so the fields stay proportional on a wide phone in landscape.
