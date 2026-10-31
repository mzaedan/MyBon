# Catat pembayaran sheet slicing plan

**Goal:** On `/Bon/Details`, **Catat pembayaran** opens the bottom sheet from `CatatHutang-export.html` (`data-pencil-name="Detail Catat Bon Catat Bayar"`): sisa hint, jumlah bayar, tanggal, catatan, and **Simpan pembayaran**.

**Source:** `CatatHutang-export.html` (screen `Detail Catat Bon Catat Bayar`).
**Depends on:** `Views/Bon/Details.cshtml`, the `.bon-picker*` sheet pattern in `Views/Bon/Create.cshtml`, tokens in `wwwroot/css/site.css`.

**Status:** visual slice only. No model, service, database, or POST action. Saving closes the sheet and waits for its own slice in `docs/plans/plan-bon-dinamis.md` (Follow-ups: *Record a payment from Details*).

## Architecture

The detail page already ships its markup. The sheet is a second overlay on the same view, moved to `.app-shell` by its own feature script so `position: absolute; inset: 0` covers the whole shell, exactly like `#bonPicker` in `Create.cshtml`.

```
BonController.Details()        unchanged (GET, View())
Views/Bon/Details.cshtml       trigger button + #bonPaySheet overlay
wwwroot/custom/features/bon/details.js   open/close, scrim, Escape, focus trap
wwwroot/css/site.css           .bon-action button reset + .bon-pay-* sheet
```

## File map

- Modify: `Views/Bon/Details.cshtml`
- Create: `wwwroot/custom/features/bon/details.js`
- Modify: `wwwroot/css/site.css`

## Slice 1: trigger

- [ ] **Step 1:** Replace the **Catat pembayaran** anchor with
  `<button type="button" class="bon-action bon-action--bayar" id="catatPembayaranBtn" aria-haspopup="dialog" aria-expanded="false" aria-controls="bonPaySheet">`.
- [ ] **Step 2:** Keep **Tambah barang** as the `Bon/Create` anchor.

## Slice 2: sheet markup

- [ ] **Step 1:** Add `#bonPaySheet` after `.bon-detail-page`, `hidden` by default: scrim, `.bon-pay-sheet` (`role="dialog"`, `aria-modal="true"`, `aria-labelledby="bonPayTitle"`, `tabindex="-1"`).
- [ ] **Step 2:** Header **Catat pembayaran** + close button (`bi-x-lg`), hint **Sisa hutang Rp 90.000. Kalau bayar kurang, masuk cicilan.**
- [ ] **Step 3:** Fields reuse `.pelanggan-field*`, `.bon-field-with-icon`, `.bon-date`, `.page-rule`: **Jumlah bayar** (`.bon-money-input`, placeholder `Tulis jumlah`), **Tanggal** (native date + `bi-calendar`), **Catatan** (`Tulis catatan (opsional)`).
- [ ] **Step 4:** Full-width `.bon-pay-submit` **Simpan pembayaran**.

## Slice 3: behaviour

- [ ] **Step 1:** `details.js` appends the sheet to `.app-shell`; opens on trigger, focuses **Jumlah bayar**, sets `aria-expanded`.
- [ ] **Step 2:** Close on scrim, close button, **Simpan pembayaran**, and `Escape`; restore focus to the trigger.
- [ ] **Step 3:** Tab/Shift+Tab stay inside the sheet, matching the picker's focus trap.
- [ ] **Step 4:** Add the script through `@section Scripts` with `asp-append-version`.

## Slice 4: styling

- [ ] **Step 1:** Add `border: 0; font-family: var(--font); cursor: pointer;` to `.bon-action` so it works as a button.
- [ ] **Step 2:** Add `.bon-pay`, `.bon-pay.is-open`, `.bon-pay-scrim`, `.bon-pay-sheet`, `.bon-pay-head`, `.bon-pay-title`, `.bon-pay-close`, `.bon-pay-hint`, `.bon-pay-submit` at the end of the Bon block.
- [ ] **Step 3:** Sheet: white surface, `16px 16px 0 0` radius, `16px` gap, `16px 20px 24px` padding, `#00000099` scrim, scrolls when the keyboard shrinks the shell.

## Verification

- [ ] `dotnet build` succeeds.
- [ ] `/Bon/Details`: **Catat pembayaran** opens the sheet; scrim darkens the shell; sheet sits on the bottom edge.
- [ ] Sheet reads the export copy; **Simpan pembayaran** is accent blue and full width.
- [ ] Close button, scrim, Escape, and save all close the sheet; focus returns to **Catat pembayaran**.
- [ ] Keyboard: Tab stays inside the sheet; second Tab pass still lands on amount, date, note, save, close.
- [ ] **Tambah barang** opens the `#bonAddSheet` slice (`docs/plans/plan-tambah-barang-slicing.md`); this slice's "still opens `/Bon/Create`" check is superseded.

## Non-goals

Real payment persistence, `bon_payment` rows, validation, amount formatting, server-side totals, per-bon data, Tailwind, Phosphor icon paths.

> Update: the native date field became the calendar popup in `docs/plans/plan-kalender-catat-pembayaran.md` — a trigger row plus a hidden ISO field (`#bonPayTanggal`) and `#bonPayCalendar` inside this sheet.

> Update (2026-10-31): the sheet is no longer visual. `.bon-pay-sheet` is a real form (`#bonPayForm`) that posts `BonId`, `Jumlah`, `Tanggal`, and `Catatan` to `POST /Bon/AddPayment`, and the redirect repaints the summary, the history, and the sisa hint from the notebook. **Simpan pembayaran** is the form's submit, so it no longer closes the sheet by script. See `docs/plans/plan-catat-pembayaran-dinamis.md`.

