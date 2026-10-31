# Validasi Jumlah bayar dalam modal plan

**Goal:** On `/Bon/Details/{id}`, the Catat pembayaran sheet explains a rejected save **under the field it
belongs to**, inside the modal. Today a save with an empty amount answers **Jumlah bayar wajib diisi** in the
page banner above the bon title (`TempData["BonError"]` -> `.bon-notice--error`), while the sheet itself stays
closed, so the message never sits beside the field that caused it.

**Depends on:** `Views/Bon/Details.cshtml` (the `#bonPaySheet` form), `Controllers/BonController.cs`
(`AddPayment` POST), `Domain/Services/BonService.cs` (`AddPaymentAsync` validation messages),
`wwwroot/custom/features/bon/details.js` (the sheet controller).

**Status:** implemented (static checks pass; the runtime checks below need the Postgres container running).

## Architecture

`AddPaymentAsync` already returns `ErrorOr.CreateValidationFailure` keyed by field (`Jumlah`, `Tanggal`), and
the request model's data annotations are keyed the same way in `ModelState`. The controller flattened all of
that into one string (`FirstErrorOrMessage` / `FirstModelStateMessage`) and wrote it to `TempData["BonError"]`,
which the view paints as a page banner. The flattening is what put a field message at the top of the page.

The fix keeps the server as the source of truth and stops flattening: each payment message rides back under
its own TempData key (`BonPayJumlahError`, `BonPayTanggalError`) and the view paints it as a
`.pelanggan-field-error` span **inside `.pelanggan-field`, between the input and its rule** — the exact markup
`Views/Barang/Create.cshtml` already uses, so the message reads where the fields already put their errors and
the existing `.pelanggan-field:has(.input-validation-error) .page-rule` rule turns that field's line red.

Because the sheet starts `hidden`, a server message also sets `data-error` on `#bonPaySheet`; `details.js`
reads it and reopens the sheet on the message. `details.js` additionally refuses an empty/zero amount before
the POST, so the common case is answered on the spot (no page reload, no banner), and clears the message the
moment the field holds a real amount.

Non-field failures keep the page banner: a bon that no longer exists (`NotFound`) and a database failure
(`Unexpected`) are not field validation, so `TempData["BonError"]` still answers them.

## File map

- Modify: `Controllers/BonController.cs` (`AddPayment` writes field-keyed TempData instead of one `BonError`)
- Modify: `Views/Bon/Details.cshtml` (error spans under Jumlah and Tanggal, `data-error` on the sheet)
- Modify: `wwwroot/custom/features/bon/details.js` (submit check, live clear, reopen on server error)
- Create: `docs/plans/plan-validasi-jumlah-bayar-dalam-modal.md` (this plan)

---

## Slice 1: controller keeps the fields apart

- [x] **Step 1:** In `AddPayment`, replace the single `TempData["BonError"]` write with
      `TempData["BonPayJumlahError"]` and `TempData["BonPayTanggalError"]`, read from `ModelState` by field
      name for a rejected bind, and from `ErrorOr.Errors` by key for a validation failure.
- [x] **Step 2:** Keep `TempData["BonError"]` for the failures that are not field validation — a missing bon
      (`NotFound`) and a database failure (`Unexpected`) — so the page banner still carries them.

## Slice 2: the view paints them under the field

- [x] **Step 1:** Read `TempData["BonPayJumlahError"]` / `TempData["BonPayTanggalError"]` in the page's `@{ }`
      block and compute the sheet's `data-error` flag.
- [x] **Step 2:** Add `<span id="bonPayJumlahError" class="pelanggan-field-error" role="alert" hidden>` between
      `#bonPayJumlah` and its `hr.page-rule`, and the same for `#bonPayTanggalError` in the Tanggal field. The
      span keeps its server text and drops `hidden` only when it has one.
- [x] **Step 3:** Add `input-validation-error` to `#bonPayTanggalBtn` when the Tanggal field carries a message,
      so the Tanggal rule reddens the way the input fields' rules already do.

## Slice 3: the script answers in place

- [x] **Step 1:** Expose `open` from `createSheet`'s controller and reopen the pay sheet when
      `#bonPaySheet` carries `data-error="true"`, focusing **Jumlah bayar**.
- [x] **Step 2:** On `#bonPayForm` submit, parse `#bonPayJumlah` (dots stripped) and, when it is empty or zero,
      show **Jumlah bayar wajib diisi** under the field, mark the field `input-validation-error`, and stop the
      submit — the double-click guard runs only after the amount passes.
- [x] **Step 3:** Clear the Jumlah message on `input` once the amount is greater than zero, and the Tanggal
      message when the calendar writes a day into `#bonPayTanggal`.

## Behaviour

| Action | Before | After |
|---|---|---|
| Save with an empty Jumlah | Banner above the title reads **Jumlah bayar wajib diisi**; sheet closed | Message under **Jumlah bayar**, inside the sheet; no banner; no POST |
| Type `1.000` after the message | — | The message disappears as the field becomes valid |
| Save `0` | Banner **Jumlah bayar wajib diisi** | Message under **Jumlah bayar**; no POST |
| Save more than the sisa | Banner **Jumlah bayar tidak boleh lebih dari sisa hutang** | Message under **Jumlah bayar**; the sheet reopens on it |
| Clear Tanggal and save | Banner **Tanggal wajib diisi** | Message under **Tanggal**; the sheet reopens on it |
| Save with a missing bon / DB failure | Banner | Banner (unchanged: not a field message) |

## Deliberately not done

- No new CSS: the message reuses `.pelanggan-field-error` and the red rule selector that already exist.
- No jQuery unobtrusive validation on the sheet: the page carries plain JS (`details.js`), and the server keeps
  the same rules as the safety net for callers that bypass the field.
- No live clamp of the amount against the sisa: the service keeps owning
  **Jumlah bayar tidak boleh lebih dari sisa hutang**, per `plan-jumlah-bayar-angka-saja.md`.
- Nothing on `/Bon/Create`'s **Total bayar** field.

## Verification

- [x] `node --check wwwroot/custom/features/bon/details.js`.
- [x] `dotnet build` succeeds with 0 warnings and 0 errors.
- [ ] `/Bon/Details/{id}` -> **Catat pembayaran** -> **Simpan pembayaran** with an empty amount shows
      **Jumlah bayar wajib diisi** under the field, inside the sheet, and no banner above the title.
- [ ] Typing `1.000` clears the message; saving writes the payment and repaints the page.
- [ ] Saving more than the sisa reopens the sheet with the message under **Jumlah bayar**.
- [ ] A Tambah barang failure still reports in the page banner.
