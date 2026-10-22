# Pilih pelanggan sheet slicing plan

**Goal:** On `/Bon/Create`, the **Pelanggan** row opens the bottom sheet from `CatatHutang-export.html` (`data-pencil-name="Catat Bon Pilih Pelanggan"`): title, search by name, customer rows with phone number and outstanding amount, and a close button. Picking a row writes the name back to the field.

**Source:** `d:\coding\Design\CatatHutang-export.html` (frame `Catat Bon Pilih Pelanggan`). The copy of the export inside the repository predates this frame and only shows the Details payment sheet.

**Depends on:** `Views/Bon/Create.cshtml`, the `.bon-picker*` overlay pattern already used by the barang picker, tokens in `wwwroot/css/site.css`.

**Status:** visual slice only. The sheet writes the picked name into the existing hidden `pelanggan` field; saving a bon, loading customers from the database, and computing sisa hutang wait for their own slices in `docs/plans/plan-bon-dinamis.md`.

## Architecture

The Create form keeps its single field row. The native `<select>` becomes a trigger button and a hidden input, so the field look matches the export and the sheet can own the list. The sheet reuses the `.bon-picker` overlay classes the barang picker already carries, plus a real search input and the customer row classes.

```
BonController.Create()                   unchanged (GET, View())
Views/Bon/Create.cshtml                  trigger + hidden field + #bonCustomerPicker
wwwroot/custom/features/bon/create.js    barang lines + both pickers (script moved out of the view)
wwwroot/css/site.css                     trigger, search input, customer rows, empty note
```

## File map

- Modify: `Views/Bon/Create.cshtml`
- Create: `wwwroot/custom/features/bon/create.js`
- Modify: `wwwroot/css/site.css`
- Modify: `docs/superpowers/specs/2026-09-27-catat-bon-form-slicing-design.md` (the native-select note is superseded)

## Slice 1: trigger and hidden field

- [x] **Step 1:** Replace the `<select id="pelanggan">` with `<input type="hidden" id="pelanggan" name="pelanggan" value="">` so the form still submits the name.
- [x] **Step 2:** Add `<button type="button" id="pilihPelangganBtn" class="bon-picker-trigger bon-customer-trigger is-empty" aria-haspopup="dialog" aria-expanded="false" aria-controls="bonCustomerPicker">` holding **Pilih pelanggan**.
- [x] **Step 3:** Swap the label to a `<span class="pelanggan-field-label" id="pelangganLabel">` and let the button read `aria-labelledby="pelangganLabel pelangganValue"`; the visible value span carries `id="pelangganValue"`, so the accessible name follows the pick.
- [x] **Step 4:** Keep the `bon-field-with-icon` wrapper and replace the chevron with `bi-caret-down` 16px muted, matching the export's caret.

## Slice 2: sheet markup

- [x] **Step 1:** Add `#bonCustomerPicker` after the barang picker, `hidden` by default: scrim, `.bon-picker-sheet` (`role="dialog"`, `aria-modal="true"`, `aria-labelledby="bonCustomerTitle"`, `tabindex="-1"`).
- [x] **Step 2:** Header **Pilih pelanggan** + close button (`bi-x-lg`, `data-bon-customer-close`).
- [x] **Step 3:** Search row: `bi-search` icon, real `<input type="search" id="bonCustomerSearch" class="bon-picker-search-input" placeholder="Cari nama">`, rule under the row.
- [x] **Step 4:** Four rows from the export, each a `.bon-customer-row` wrapper holding a `.bon-picker-item.bon-customer-item` button and a rule: Siti Aminah / `0812 3456 7890` / `Rp 185.000`, Budi Santoso / `0813 2211 4455` / `Rp 92.000`, Rina Wahyuni / `0857 9900 1122` / `Rp 48.500`, Agus Salim / `0819 7788 3344` / `Rp 0`. Each button carries `data-nama`, `data-hp`, and `data-sisa`.
- [x] **Step 5:** The `Rp 0` row wears `.bon-customer-item-sisa--lunas` so the amount reads muted like the export.
- [x] **Step 6:** Hidden `.bon-customer-empty` note **Belum ada pelanggan yang cocok.** for a search with no match.

## Slice 3: behaviour

- [x] **Step 1:** Move the inline Create script into `wwwroot/custom/features/bon/create.js`, keeping the barang line behaviour unchanged, and register it through `@section Scripts` with `asp-append-version`.
- [x] **Step 2:** Append `#bonCustomerPicker` to `.app-shell`; open on the trigger, focus **Cari nama**, set `aria-expanded`.
- [x] **Step 3:** Close on scrim, close button, `Escape`, and a pick; restore focus to the trigger.
- [x] **Step 4:** Tab/Shift+Tab stay inside the sheet, matching the barang picker's focus trap.
- [x] **Step 5:** Typing in **Cari nama** filters rows by customer name (case-insensitive); a pick closes the sheet; closing clears the term so the next open shows all four.
- [x] **Step 6:** A pick writes `data-nama` into the hidden input, updates the trigger text, and removes `is-empty` so the field turns black.


## Slice 4: styling

- [x] **Step 1:** Add `.bon-customer-trigger` beside `.bon-picker-trigger`: `min-height: 0` and `padding: 0 24px 0 0` so the form row keeps the export's compact height instead of the sheet rows' 44px target.
- [x] **Step 2:** Group `.bon-picker-search-input` with `.bon-add-search-input` (plus `::placeholder`, `:focus`) — transparent, borderless 15px field filling the row.
- [x] **Step 3:** Add `.bon-picker-search:focus-within .page-rule` to the add-sheet focus rule so the rule darkens while typing.
- [x] **Step 4:** Add `.bon-customer-row[hidden]`, `.bon-customer-empty`, `.bon-customer-empty[hidden]`, and `.bon-customer-item-sisa--lunas` (`color: var(--muted)`); the rest of the row reuses `.bon-picker-item*`.

## Verification

Checked on the machine with `dotnet run` and headless Chrome over CDP:

- [x] `node --check wwwroot/custom/features/bon/create.js` passes.
- [x] `dotnet build` succeeds: 0 warnings, 0 errors.
- [x] `GET /Bon/Create` returns 200 with `#pilihPelangganBtn`, `#pelanggan`, `#bonCustomerPicker`, `#bonCustomerSearch`, and four `.bon-customer-row` nodes.
- [x] **Pilih pelanggan** opens the sheet, sets `hidden=false` / `is-open` / `aria-expanded="true"`, and focuses the search box (`document.activeElement` = `bonCustomerSearch`).
- [x] Typing `budi` leaves only **Budi Santoso**; typing `zzz` shows **Belum ada pelanggan yang cocok.**
- [x] Picking **Siti Aminah** closes the sheet, writes `Siti Aminah` into the hidden field, and the trigger colour moves from `rgb(92, 92, 92)` to `rgb(0, 0, 0)`.
- [x] Escape and the scrim close the sheet and return focus to **Pilih pelanggan**; the next open shows an empty search box and all four rows.
- [x] The barang picker still opens and the subtotal arithmetic still works (`Beras Ramos 5 kg` → `Rp 78.000`, total `Rp 78.000`).
- [x] Computed metrics match the export where checked: trigger height 22.39px, search input 15px, sheet radius `16px 16px 0 0`, sisa uses Fraunces, scrim `rgba(0, 0, 0, 0.6)`, **Rp 0** row muted.

## Non-goals

Loading customers from `IPelangganService`, real sisa hutang, customer search on the server, POST wiring, a new JS library, Tailwind, Phosphor icon paths.
