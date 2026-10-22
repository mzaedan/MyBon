# Tambah barang sheet slicing plan

**Goal:** On `/Bon/Details`, **Tambah barang** opens the bottom sheet from `CatatHutang-export.html` (`data-pencil-name="Detail Catat Bon Tambah Barang"`): title, search row, eight item rows with satuan and price, and a close button.

**Source:** `d:\coding\Design\CatatHutang-export.html` (screen `Detail Catat Bon Tambah Barang`). The copy of the export inside the repository predates this frame and still shows the payment sheet only.

**Depends on:** `Views/Bon/Details.cshtml`, the `#bonPaySheet` overlay added by `docs/plans/plan-catat-pembayaran-slicing.md`, `.bon-picker*` in `Views/Bon/Create.cshtml`, tokens in `wwwroot/css/site.css`.

**Status:** visual slice only. No model, service, database, or POST action. Picking a row closes the sheet; writing the line into the bon waits for its own slice in `docs/plans/plan-bon-dinamis.md` (Follow-ups).

## Architecture

The detail page already carries one overlay. This is the second, moved to `.app-shell` by the same feature script so `position: absolute; inset: 0` covers the whole shell.

```
BonController.Details()                  unchanged (GET, View())
Views/Bon/Details.cshtml                 trigger button + #bonAddSheet overlay
wwwroot/custom/features/bon/details.js   one sheet controller used by both overlays, local search filter
wwwroot/css/site.css                     .bon-add-* sheet, search, item rows
```

## File map

- Modify: `Views/Bon/Details.cshtml`
- Modify: `wwwroot/custom/features/bon/details.js`
- Modify: `wwwroot/css/site.css`
- Modify: `docs/plans/plan-catat-pembayaran-slicing.md` (its "Tambah barang still opens `/Bon/Create`" verification is superseded here)

## Slice 1: trigger

- [x] **Step 1:** Replace the **Tambah barang** anchor with
  `<button type="button" class="bon-action bon-action--barang" id="tambahBarangBtn" aria-haspopup="dialog" aria-expanded="false" aria-controls="bonAddSheet">`.
- [x] **Step 2:** Keep the `bi-plus` icon and the label exactly as they are.

## Slice 2: sheet markup

- [x] **Step 1:** Add `#bonAddSheet` after `#bonPaySheet`, `hidden` by default: scrim, `.bon-add-sheet` (`role="dialog"`, `aria-modal="true"`, `aria-labelledby="bonAddTitle"`, `tabindex="-1"`).
- [x] **Step 2:** Header **Tambah barang** + close button (`bi-x-lg`, `data-bon-add-close`).
- [x] **Step 3:** Search row: `bi-search` icon, real `<input type="search" id="bonAddSearch" placeholder="Cari barang">`, rule under the row.
- [x] **Step 4:** Eight rows from the export, each a `.bon-add-row` wrapper holding a `.bon-add-item` button and a rule: Beras Ramos 5 kg / karung / `Rp 78.000`, Minyak Goreng 2 L / botol / `Rp 34.000`, Gula Pasir / kg / `Rp 17.500`, Telur Ayam / kg / `Rp 28.000`, Mie Instan / dus / `Rp 125.000`, Kopi Sachet / renceng / `Rp 12.000`, Sabun Mandi / pcs / `Rp 4.500`, Garam Dapur / bungkus / `Rp 3.000`. Each button carries `data-nama`, `data-satuan`, and `data-harga` like the Create picker.
- [x] **Step 5:** Hidden `.bon-add-empty` note **Belum ada barang yang cocok.** for a search with no match.

## Slice 3: behaviour

- [x] **Step 1:** `details.js` keeps one controller per sheet: append to `.app-shell`, open on trigger, close on scrim, close button, `Escape`, and the sheet's own dismiss action, restore focus, set `aria-expanded`.
- [x] **Step 2:** Opening one sheet closes the other, so no scrim stacks.
- [x] **Step 3:** Focus lands on the search input when the add sheet opens, and Tab/Shift+Tab stay inside the sheet.
- [x] **Step 4:** Typing in **Cari barang** filters rows by item name (case-insensitive); a pick closes the sheet; closing clears the term so the next open shows all eight.
- [x] **Step 5:** Reuse the one `@section Scripts` registration; no second script file.

## Slice 4: styling

- [x] **Step 1:** Add `.bon-add`, `.bon-add.is-open`, `.bon-add-scrim`, `.bon-add-sheet` at the end of the Bon block: white surface, `16px 16px 0 0` radius, `12px` gap, `16px 20px 24px` padding, `#00000099` scrim, `max-height: min(70%, calc(100% - 72px))`, list scrolls.
- [x] **Step 2:** Add `.bon-add-head`, `.bon-add-title` (Figtree 18px semibold), `.bon-add-close` (44px, `bi-x-lg` 20px).
- [x] **Step 3:** Add `.bon-add-search*`: 18px muted icon, borderless 15px input, placeholder muted, `:focus-within .page-rule` darkens the rule.
- [x] **Step 4:** Add `.bon-add-list`, `.bon-add-row`, `.bon-add-item` (padding `12px 0`, space-between), name 15px semibold, satuan 12px muted, price Fraunces 16px semibold; `[hidden]` rows stay hidden.

## Verification

Checked on the machine with `dotnet run` and headless Chrome over CDP:

- [x] `node --check wwwroot/custom/features/bon/details.js` passes.
- [x] `dotnet build` succeeds: 0 warnings, 0 errors.
- [x] `GET /Bon/Details` returns 200 with `#tambahBarangBtn`, `#bonAddSheet`, `#bonAddSearch`, and eight `.bon-add-row` nodes.
- [x] The button is no longer an anchor to `/Bon/Create`; no database or POST action was added.
- [x] **Tambah barang** sets `hidden=false`, `is-open`, `aria-expanded="true"`, moves the overlay under `.app-shell`, and focuses `#bonAddSearch`.
- [x] Typing `telur` leaves only **Telur Ayam**; typing `zzz` leaves zero rows and shows **Belum ada barang yang cocok.**
- [x] Escape closes the sheet, resets `aria-expanded`, and returns focus to **Tambah barang**; reopening shows an empty search and all eight rows.
- [x] Picking a row closes the sheet and returns focus; **Catat pembayaran** still opens its own sheet, closes this one, and restores focus to its trigger on close.
- [x] Computed style matches the export: 18px/600 title, 15px/600 name, 12px satuan, Fraunces 16px/600 price, `16px 16px 0 0` sheet radius, `#00000099` scrim.

Still open:

- [ ] A human visual pass over `/Bon/Details` for pixel spacing against the export frame.

## Non-goals

Writing the picked item into the bon, quantity, subtotal, totals, price reads from `barang`, server-side search, validation, Tailwind, Phosphor icon paths, a new JS library.
