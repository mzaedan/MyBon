# Jumlah bayar angka saja plan

**Goal:** Keep letters and other symbols out of **Jumlah bayar** on the Catat pembayaran sheet at
`/Bon/Details/{id}`, so the amount can only be typed as digits, dots, and a comma — the same behaviour
the **Harga** field already has on `/Barang/Create` and `/Barang/Edit`.

**Spec:** `docs/plans/plan-barang-harga-angka-saja.md` (the behaviour this plan copies),
`docs/plans/plan-catat-pembayaran-dinamis.md` (the sheet and its POST)

**Status:** implemented on 2026-10-31. `#bonPayJumlah` now carries `data-money-input` and the page loads
the shared helper, so a typed letter never reaches the field and the posted value stays plain digits.

## Architecture

The behaviour already lived in one script, `wwwroot/custom/features/barang/harga-input.js`, bound to any
field marked `data-money-input`. That folder is barang-specific, and this plan makes a second feature use
it, so the script moved to `wwwroot/helpers/money-input.js` (AGENTS.md: reusable utilities shared across
features live in `wwwroot/helpers/`). Nothing else changes: `details.js` keeps owning the sheet and its
submit guard, `AddBonPaymentRequest.Jumlah` keeps the typed text, and `TryParseMoney` in
`Domain/Services/BonService.cs` keeps parsing it on the server.

The separator is display only. On submit the helper strips the dots, so `1.000` posts as `1000` and
`1.000,5` as `1000,5` — both of which `TryParseMoney` already reads, and neither of which form binding
misreads as `1` or `1000,5` with a server culture.

Reusing the helper instead of writing a Bon copy keeps one rule for money fields: a fix to the caret
handling or the paste cleanup lands on all three forms at once.

## File map

- Move: `wwwroot/custom/features/barang/harga-input.js` -> `wwwroot/helpers/money-input.js` (git mv, history kept)
- Modify: `wwwroot/helpers/money-input.js` (header comment names the three forms it serves)
- Modify: `Views/Barang/Create.cshtml`, `Views/Barang/Edit.cshtml` (`<script src>` points at the helper)
- Modify: `Views/Bon/Details.cshtml` (`data-money-input` on `#bonPayJumlah`, helper loaded in `@section Scripts`)
- Modify: `docs/plans/plan-barang-harga-angka-saja.md` (script path update note)
- Create: `docs/plans/plan-jumlah-bayar-angka-saja.md` (this plan)

---

## Slice 1: the shared helper moves to `wwwroot/helpers/`

- [x] **Step 1:** `git mv wwwroot/custom/features/barang/harga-input.js wwwroot/helpers/money-input.js`,
      so the file keeps its history and sits where cross-feature JS belongs.
- [x] **Step 2:** Rewrite the header comment to say what it does (numeric money field, grouped
      thousands, dots stripped on submit) and which forms use it, instead of naming only Barang.
- [x] **Step 3:** Point `Views/Barang/Create.cshtml` and `Views/Barang/Edit.cshtml` at
      `~/helpers/money-input.js`. `asp-append-version` stays, so the browser asks for the new path.

## Slice 2: the payment field joins in

- [x] **Step 1:** Add `data-money-input` to `#bonPayJumlah` in `Views/Bon/Details.cshtml`, beside the
      `inputmode="decimal"` and `maxlength="20"` it already had. Keeping the limit means the grouped
      value still fits the request's 20-character cap.
- [x] **Step 2:** Load `~/helpers/money-input.js` in the page's `@section Scripts`, after
      `bon-calendar.js` and before `details.js`, so the sheet still opens on the field it always did.
- [x] **Step 3:** Leave `details.js`, `AddBonPaymentRequest`, `BonController.AddPayment`, and
      `BonService.AddPaymentAsync` untouched: the sheet's submit, the double-click guard, the parsing,
      and the rejection messages already behave; only what may enter the field changed.

## Behaviour

| Action | Before | After |
|---|---|---|
| Type `abc` | Field shows `abc`; server answers **Jumlah bayar harus angka, contoh 50.000** | Nothing appears in the field |
| Type `-` | Field shows `-`; server rejects on save | Nothing appears in the field |
| Type `1000` | Field shows `1000`; posts `1000` | Field shows `1.000`; posts `1000` |
| Paste `Rp 28.000` | Field keeps `Rp 28.000`; server rejects on save | Field shows `28.000`; posts `28000` |
| Paste `1.000abc500` | Field keeps the letters; server rejects on save | Field shows `1.000.500` |
| Type `1000,5` | Field shows `1000,5`; posts `1000,5` | Field shows `1.000,5`; posts `1000,5` |

The server-side parse and its messages stay as the safety net for callers that bypass the field (a
direct `POST /Bon/AddPayment`, an AJAX caller), as `plan-barang-harga-angka-saja.md` decided for Harga.

## Deliberately not done

- `type="number"`: it drops thousand separators, blocks the comma in some browsers, and reintroduces
  culture-dependent parsing.
- A Bon-only copy of the script: one behaviour, one file.
- The **Total bayar** field on `/Bon/Create`: `plan-barang-harga-angka-saja.md` left it out of scope,
  and this request named the payment sheet only.
- Live formatting of the **Sisa hutang** hint or a max-value clamp in the field: the service keeps
  owning that rule (**Jumlah bayar tidak boleh lebih dari sisa hutang**), so the field never disagrees
  with the notebook.

## Verification

- [x] `node --check wwwroot/helpers/money-input.js`.
- [x] `dotnet build` succeeds with 0 warnings and 0 errors.
- [x] No view still references `~/custom/features/barang/harga-input.js`.
- [ ] `/Bon/Details/{id}` -> **Catat pembayaran**: typing `dua ribu` leaves the field empty; typing
      `1000` shows `1.000`; **Simpan pembayaran** writes `1000` and the page reads
      **Pembayaran Rp 1.000 dicatat**.
- [ ] Pasting `Rp 28.000` leaves `28.000` in the field and saves 28000.
- [ ] `/Barang/Create` and `/Barang/Edit/{id}` still behave as before (letters blocked, `34.000`
      prefilled and saved as 34000) after the script moved.
