# Catat pembayaran dinamis plan

**Goal:** On `/Bon/Details`, the **Catat pembayaran** sheet writes a real `bon_payment` row, so Ringkasan, Riwayat pembayaran, the sisa hint, and the list card's totals change with the notebook. The sheet shipped as a visual slice in `docs/plans/plan-catat-pembayaran-slicing.md` and its calendar in `docs/plans/plan-kalender-catat-pembayaran.md`; both wrote nothing.

**Depends on:** `Views/Bon/Details.cshtml`, `BonService.GetDetailAsync` and its `bon_payment` sums (`docs/plans/plan-bon-dinamis.md`), the sheet controller in `wwwroot/custom/features/bon/details.js`, `TryParseMoney` in `Domain/Services/BonService.cs`.

**Status:** implemented and verified against Postgres and headless Chrome on 2026-10-31. `POST /Bon/AddPayment` writes the row and redirects back to the page; a rejected save lands on the same page with a notice.

## Architecture

```
POST /Bon/AddPayment   AddBonPaymentRequest      -> BonService.AddPaymentAsync -> bon_payment row
                                                 -> redirect /Bon/Details/{id} (TempData notice)
GET  /Bon/Details/{id} BonService.GetDetailAsync -> the repainted sums, history, and sisa hint
```

One save is one `bon_payment` row. Totals stay derived (`SUM(jumlah)`), so the redirect repaints the
summary, the history, and the sheet's hint from one source, exactly like `POST /Bon/AddItem` does for
lines. The amount is never posted as a decimal: form binding would parse `"50.000"` with the server
culture and read `50`, so the request keeps the typed text and the service parses it with the same
`TryParseMoney` the Create form uses.

## File map

- Create: `Models/Bon/Requests/AddBonPaymentRequest.cs`
- Create: `docs/plans/plan-catat-pembayaran-dinamis.md` (this plan)
- Modify: `Domain/Interfaces/IBonService.cs`, `Domain/Services/BonService.cs` (`AddPaymentAsync`)
- Modify: `Controllers/BonController.cs` (`AddPayment` POST, shared field-message helpers)
- Modify: `Views/Bon/Details.cshtml` (the sheet becomes `#bonPayForm`)
- Modify: `wwwroot/custom/features/bon/details.js` (save is a submit, not a close)
- Modify: `docs/plans/plan-catat-pembayaran-slicing.md`, `plan-kalender-catat-pembayaran.md`,
  `plan-tambah-barang-dinamis.md`, `plan-bon-dinamis.md` (their "writes nothing" notes)


## Slice 1: request and write path

- [x] **Step 1:** `AddBonPaymentRequest` carries `BonId`, `Jumlah` (typed text, max 20), `Tanggal`
      (required), and `Catatan` (optional, max 200 — the column's length).
- [x] **Step 2:** `AddPaymentAsync` parses the amount with `TryParseMoney`, rounds to the two decimals
      the money columns keep, and refuses: not a number (**Jumlah bayar harus angka, contoh 50.000**),
      empty or zero (**Jumlah bayar wajib diisi**), and over the column (**Jumlah bayar terlalu besar**).
- [x] **Step 3:** The service re-reads `SUM(harga_satuan * jumlah)` and `SUM(jumlah)` for the bon, so
      the sisa it validates against is the same figure the page prints. A missing bon is
      `ErrorType.NotFound`; a paid-off bon answers **Bon ini sudah lunas**; an amount above the sisa
      answers **Jumlah bayar tidak boleh lebih dari sisa hutang**, so a bon cannot turn into a credit.
- [x] **Step 4:** One `bon_payment` row is written with the posted date and note; a database failure is
      logged and answered with **Gagal menyimpan pembayaran. Coba simpan lagi.**
- [x] **Step 5:** The success message reads what the repainted page shows:
      **Pembayaran Rp 50.000 dicatat. Sisa Rp 40.000** (or **… Bon ini sudah lunas**).

## Slice 2: controller

- [x] **Step 1:** `POST /Bon/AddPayment` is anti-forgery protected and redirects back to
      `Details/{id}`; a rejected save lands on the same page with `TempData["BonError"]` instead of a
      bare status code, and a success lands with `TempData["BonStatus"]`.
- [x] **Step 2:** `BonId <= 0` sends the caller to `/Bon`, and a bon that no longer exists
      (`ErrorType.NotFound`) does the same, because there is no page to return to.
- [x] **Step 3:** The first field message wins over the generic `ErrorOr` message, so the notice reads
      the field's own wording. The two lookups moved into `FirstModelStateMessage()` /
      `FirstErrorOrMessage(ErrorOr)`, and `AddItem` uses them too instead of repeating the query.

## Slice 3: sheet

- [x] **Step 1:** `.bon-pay-sheet` became the `<form method="post" asp-action="AddPayment"
      id="bonPayForm">`, so the sheet keeps its flex layout, the tag helper writes the anti-forgery
      token, and the close/scrim selectors still find the panel.
- [x] **Step 2:** The fields post their own names — `BonId` (hidden), `Jumlah`, `Tanggal` (the hidden
      ISO field the calendar writes), `Catatan` — and **Simpan pembayaran** is now `type="submit"`
      instead of a close button.
- [x] **Step 3:** `details.js` no longer treats `[data-bon-pay-save]` as a close selector, so a save
      leaves the form to the browser; a `submit` guard stops a second click before the POST lands.

## Verification (Postgres + headless Chrome, 2026-10-31)

- [x] `dotnet build` succeeds: 0 warnings, 0 errors. `node --check details.js` passes.
- [x] `/Bon/Details/14` rendered `<form method="post" id="bonPayForm" class="bon-pay-sheet" action="/Bon/AddPayment">`
      with `BonId=14`, `name="Jumlah"`, `name="Tanggal"` (`2026-10-31`), `name="Catatan"`, and an
      anti-forgery token.
- [x] Saving **2.000** with the note **Test cicilan** answered `302 -> /Bon/Details/14` and wrote
      `bon_payment(bon_id 14, 2026-10-31, 2000.00, 'Test cicilan')`. The page then read
      **Pembayaran Rp 2.000 dicatat. Sisa Rp 2.500**, **Cicilan 1 / Rp 2.000**, and the hint
      **Sisa hutang Rp 2.500. Kalau bayar kurang, masuk cicilan.**
- [x] Paying the rest (**2.500**) read **Pembayaran Rp 2.500 dicatat. Bon ini sudah lunas**, the status
      flipped to **Lunas**, and the hint to **Bon ini sudah lunas.** A further save answered
      **Bon ini sudah lunas** and wrote nothing.
- [x] Rejections each wrote no row: empty amount **Jumlah bayar wajib diisi**; **abc**
      **Jumlah bayar harus angka, contoh 50.000**; **99.999** against a Rp 2.500 sisa
      **Jumlah bayar tidak boleh lebih dari sisa hutang**; cleared date **Tanggal wajib diisi**;
      `BonId=9999` redirected to `/Bon` with **Bon tidak ditemukan.**
- [x] Headless Chrome: **Catat pembayaran** opened the sheet with focus on **Jumlah bayar**, typing
      **1.000** and clicking **Simpan pembayaran** repainted the page with
      **Pembayaran Rp 1.000 dicatat. Sisa Rp 3.500** and a **Rp 1.000** history row, so the sheet, the
      POST, and the redirect agree in a real browser.
- [x] Every test payment was deleted again, so `bon_payment` is empty and **BN-001** still owes
      **Rp 4.500**.

## Non-goals

Editing or deleting a recorded payment, a payment dated before the bon, min/max dates, a
transaction-scoped re-read for two saves racing on one bon, repainting through AJAX instead of the
redirect, showing `Catatan` in the history rows, stock movement, a new JS library.

