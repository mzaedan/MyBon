# Kelola pengguna slicing plan

**Goal:** Re-slice the account management list at `/Pengguna` from the newest `CatatHutang-export.html` (frames `Kelola Pengguna` and `Kelola Pengguna - Atur Ulang Kata Sandi`): a name with an active/non-active stamp, the role and last-seen lines, a `Atur ulang kata sandi` link beside an outlined `Nonaktifkan` button, the expanded reset form that replaces that action row, and the primary `Buat akun admin` CTA.

**Source:** `CatatHutang-export.html` (frames `Kelola Pengguna` and `Kelola Pengguna - Atur Ulang Kata Sandi`, 390 wide).

**Depends on:** `Views/Pengguna/Index.cshtml`, `Controllers/PenggunaController.cs`, `IPenggunaService`, the `.page-rule` / `.cta-catat-bon` / `.pelanggan-*` tokens in `wwwroot/css/site.css`, the shell in `Views/Shared/_Layout.cshtml`.

**Status:** visual slice. The list keeps its real data and its POST wiring (`ResetPassword`, `SetAktif`, antiforgery, `data-confirm`); no controller, service, model, or database change.

## Architecture

The page keeps one form per account action, so the existing endpoints stay untouched. The reset password form stays inside a native `<details>`: closed, its summary is the export's `Atur ulang kata sandi` link; open, it becomes the form title and paints the 48px password field plus the `Batal` / `Simpan` pair, while the enable/disable form is hidden for that account. The enable/disable form keeps its hidden `id`/`aktif` fields and only changes the button label and outline colour.

```
PenggunaController.Index()                 unchanged
Views/Pengguna/Index.cshtml                export order: rule, notices, account rows, CTA
wwwroot/custom/features/pengguna/index.js  confirm prompt + reset toggle/Batal focus
wwwroot/css/site.css                       .pengguna-* block replaced with export metrics
```

## File map

- Modify: `Views/Pengguna/Index.cshtml`
- Modify: `wwwroot/custom/features/pengguna/index.js`
- Modify: `wwwroot/css/site.css`
- Create: `docs/plans/plan-pengguna-slicing.md`

## Slice 1: account row markup

- [x] **Step 1:** Wrap the page in `.pengguna-page` (10px column gap, 24px bottom padding) and keep the `.page-rule` header separator.
- [x] **Step 2:** Keep the status/error notices and reuse `.pelanggan-notice` / `.pelanggan-notice--error`.
- [x] **Step 3:** Each account paints `.pengguna-card-top` with `.pengguna-card-name` and a `.pengguna-badge` (44x24 pill, `Aktif` accent / `Nonaktif` muted).
- [x] **Step 4:** Paint `.pengguna-card-role` (`@username · Peran`) and `.pengguna-card-activity` (`Terakhir masuk ...`, or `Belum pernah masuk`).
- [x] **Step 5:** Paint `.pengguna-card-actions` with the `<details class="pengguna-reset">` form and the `SetAktif` form; the button reads `Nonaktifkan` on an active account and `Aktifkan` on a disabled one.
- [x] **Step 6:** Paint a `.pengguna-rule` between accounts only, not after the last one.
- [x] **Step 7:** Close with `.cta-catat-bon .pengguna-cta` labelled `Buat akun admin`.
- [x] **Step 8:** Inside the reset details, paint `.pengguna-reset-input` (48px) and `.pengguna-reset-actions` with `Batal` (`data-pengguna-reset-cancel`) and `Simpan` (submit); the expanded frame replaces the action row, so the status form hides while it is open.

## Slice 2: styling

- [x] **Step 1:** `.pengguna-card`: 9px vertical padding, 6px gap, no card box (the export uses rules, not cards).
- [x] **Step 2:** Type: name 16px/400, role 13px, activity 12px, pill 11px/600, reset link 13px (`#145FBD`).
- [x] **Step 3:** `.pengguna-reset summary` is a 44px tap target with its marker hidden; when open it becomes a 14px/600 title with an 8px gap, the details spans the card, and `.pengguna-card-actions:has(...)` hides the status form.
- [x] **Step 4:** The open form: `.pengguna-reset-input` is 48px high, 8px radius, 1px `#BFC7D1` outline, 14px text; `Batal` and `Simpan` split the row at 44px with an 8px gap (`#17324D` outline / accent fill).
- [x] **Step 5:** `.pengguna-status-btn` is 98x44, 8px radius, white surface, 1px outline: `#B9002A`/`#A40024` for `Nonaktifkan`, accent for `Aktifkan`.
- [x] **Step 6:** `.pengguna-rule` uses the export's `#E8EBEF`; the CTA label drops to the export's 15px/400.
- [x] **Step 7:** Every interactive element keeps the app's 2px `:focus-visible` ring.

## Verification

- [x] `dotnet build` succeeds.
- [x] `node --check wwwroot/custom/features/pengguna/index.js` passes.
- [x] `GET /Pengguna` renders one `.pengguna-card` per account with the badge, both meta lines, the reset summary, and the status button.
- [x] No horizontal overflow at 390px; the CTA and last account stay above the bottom nav.
- [x] `Nonaktifkan` still posts `SetAktif` with the confirm prompt; `Atur ulang kata sandi` opens the expanded form (title, 48px field, `Batal` / `Simpan`), hides the status button, and `Simpan` still posts `ResetPassword`.
- [x] `Batal` closes the form without posting, the status button returns, and focus goes back to the summary; opening focuses the password field.
- [x] Keyboard: summary, status button, `Batal`, and `Simpan` are focusable with a visible ring.

## Non-goals

Header/shell changes, a reset-password bottom sheet, server-side validation changes, new JS libraries, Tailwind, and Phosphor icon paths.

## Result

Checked with `dotnet build`, the app on `http://localhost:5032`, and headless Chrome at 390 x 844:

- `dotnet build` — 0 warnings, 0 errors. `node --check wwwroot/custom/features/pengguna/index.js` passes.
- `GET /Pengguna` answers 200 with four `.pengguna-card` rows, three `.pengguna-rule` dividers, and one `.pengguna-cta`.
- Computed metrics: card 350px wide, badge 44x24, name 16px/400, role 13px, activity 12px, summary 13px `#145FBD`, status button 98x44 with a `#B9002A` outline and `#A40024` label, CTA 48px / 15px / 400.
- Document scroll width stays 390px in both states, so nothing overflows sideways.
- Expanded reset form: title 14px/600 with an 8px gap, field 350x48 with a `#BFC7D1` outline and 14px text, `Batal` / `Simpan` 171x44 each with an 8px gap; the status button is `display:none`, focus lands in the field, and `Batal` restores the closed row with focus on the summary.
- `SetAktif` through the form returned 302 and flipped `windah` to inactive (badge `Nonaktif`, button `Aktifkan`); a second post restored it. The test rows and admin hash were restored afterwards.
- `ResetPassword` with a short password returned 302 and the page showed an error notice. Its generic `Validation failed.` text comes from the existing controller notice, not from this slice.
- The shared header still paints the earlier slice (name chip, 32px icon buttons). Aligning it to the export's 44px icon-only actions is left out on purpose (see Non-goals).