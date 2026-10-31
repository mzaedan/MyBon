# Tambah barang stepper slicing plan

**Goal:** On `/Bon/Details`, the **Tambah barang** sheet matches the current export frame: each stock row
carries a quantity stepper (`− 1 +`) beside its price, and a pick writes the count the stepper shows
instead of always one unit.

**Source:** `CatatHutang-export.html` in the repository root (screen `Detail Catat Bon Tambah Barang`).

**Depends on:** `Views/Bon/Details.cshtml`, `Views/Bon/_BarangRows.cshtml` (shared with the Create
picker), `docs/plans/plan-tambah-barang-dinamis.md`, tokens in `wwwroot/css/site.css`.

**Status:** implemented on 2026-10-31. The sheet paints the same rows it already listed, now with the
stepper the export draws, and `POST /Bon/AddItem` writes the count the row showed. Build and script
checks pass; the browser pass against the export frame is still open.

## Architecture

```
GET  /Bon/Details/{id}                      -> <partial _BarangRows> with ViewData["Stepper"] = true
GET  /Bon/SearchBarang?q&page&stepper=true  -> _BarangRows with one stepper per row (Details sheet)
GET  /Bon/SearchBarang?q&page               -> _BarangRows without it (Create picker, unchanged)
POST /Bon/AddItem  { BonId, BarangId, Jumlah }
```

One partial paints both item pickers, so a `stepper` flag decides the row's shape instead of a second
copy of the markup. The stepper is a real control: it only sets `Jumlah`, and the pick button beside it
submits `#bonAddForm`, so the count lands in the notebook and the redirect repaints Ringkasan, Daftar
barang, and Total belanja from the same source as before.

## File map

- Modify: `Views/Bon/_BarangRows.cshtml` (row shape, stepper markup behind the flag)
- Modify: `Views/Bon/Details.cshtml` (`ViewData["Stepper"]`, hidden `Jumlah`, `stepper=true` search URL)
- Modify: `Controllers/BonController.cs` (`SearchBarang` flag, first field message on a rejected pick)
- Modify: `Models/Bon/Requests/AddBonItemRequest.cs` (`Jumlah`, 1..9999, default 1)
- Modify: `Domain/Services/BonService.cs` (`AddItemAsync` writes `request.Jumlah`)
- Modify: `wwwroot/custom/features/bon/details.js` (stepper delegation, pick posts the count)
- Modify: `wwwroot/css/site.css` (`.bon-add-item`, `.bon-add-stepper`, `.bon-add-step*`)
- Modify: `docs/plans/plan-tambah-barang-dinamis.md` (its "editing the quantity" non-goal is superseded)

## Slice 1: row markup

- [x] **Step 1:** Every `_BarangRows` row keeps the `.bon-picker-item` button and gains a `.bon-add-item`
      wrapper, so the pick and the stepper can sit side by side without nesting buttons.
- [x] **Step 2:** When `ViewData["Stepper"] is true`, the wrapper also renders `.bon-add-stepper`
      (`role="group"`, `aria-label="Jumlah <nama>"`): minus (`data-bon-step="-1"`), the value
      (`[data-bon-step-value]`, `aria-live="polite"`, starts at 1), plus (`data-bon-step="1"`), each a
      `type="button"` with its own `aria-label`.
- [x] **Step 3:** The Create picker asks for the same partial without the flag, so its rows keep their
      current shape and one tap still fills a line's item.

## Slice 2: write path

- [x] **Step 1:** `#bonAddForm` carries a hidden `Jumlah` (`id="bonAddJumlah"`, `value="1"`).
- [x] **Step 2:** `AddBonItemRequest.Jumlah` is `[Range(1, 9999)]` with a default of 1, so an old caller
      that posts only `BonId` and `BarangId` still writes one unit.
- [x] **Step 3:** `AddItemAsync` writes `Jumlah = request.Jumlah > 0 ? request.Jumlah : 1` and still
      copies the name, unit, and price from `barang`.
- [x] **Step 4:** A rejected POST reports the first field message, so an out-of-range count no longer
      reads **Barang wajib dipilih**.
- [x] **Step 5:** `SearchBarang` takes `stepper` and hands it to the partial through `ViewData`; the
      Details sheet's `data-search-url` already asks for `stepper=true`, so search and load-more pages
      keep their steppers.

## Slice 3: behaviour

- [x] **Step 1:** `details.js` delegates clicks on `[data-bon-step]` on `.bon-add-list`, so the handler
      survives every search re-render.
- [x] **Step 2:** The count is read from the row, stepped by ±1, and clamped to 1..9999; both buttons
      stay enabled and the value simply stops at the edges.
- [x] **Step 3:** A pick copies the count into `#bonAddJumlah` before submitting; the stepper itself never
      submits and never adds a line.
- [x] **Step 4:** `resetAddSearch` keeps clearing the term on close; the next open paints every row at 1.

## Slice 4: styling

- [x] **Step 1:** `.bon-add-item` is the export's row: flex, `10px` gap, `12px 0` padding inside
      `.bon-add-sheet` only, so the Create picker's row keeps the padding `.bon-picker-item` always had.
- [x] **Step 2:** Inside the sheet the pick button takes the remaining width (`flex: 1 1 auto`,
      `min-width: 0`, no own padding) and a long name truncates instead of pushing the stepper away.
- [x] **Step 3:** `.bon-add-stepper` is `36px` tall, `8px` radius, `#E5E5E5` hairline outline at
      `-0.5px`; the two 30px buttons use 14px `bi-dash` (muted) and `bi-plus` (text) icons, the value is
      a 24px centred 15px/600 label. Focus rings use the accent colour.

## Verification

Checked on 2026-10-31 against Postgres on `localhost:5439` with `dotnet build -c Release`, `node --check`,
and headless Chrome over CDP at a 390px viewport.

- [x] `dotnet build -c Release` succeeds: 0 warnings, 0 errors.
      (A Debug build cannot copy `bin/Debug/net10.0/MyBon.*` while the shopkeeper's app is running.)
- [x] `node --check wwwroot/custom/features/bon/details.js` passes.
- [x] `/Bon/Details/16` renders 11 stock rows, each with `.bon-add-item` and `.bon-add-stepper`.
- [x] The sheet opens from **Tambah barang**: `hidden=false`, `is-open`, `aria-expanded="true"`, focus
      on `#bonAddSearch`; no script errors.
- [x] The stepper steps and clamps: `+ + +` -> `4`, `−` -> `3`, four `−` -> `1` (floor), `+ +` -> `3`.
- [x] Typing **kopi** painted `kopi`, `kopi gooday`, `kopi sachet`, each with its own stepper back at 1,
      so search and load-more pages keep the steppers the flag asks for.
- [x] `GET /Bon/SearchBarang?q=kopi&stepper=true` answers stepper rows; the same call without the flag
      answers the Create picker's shape (0 steppers).
- [x] `/Bon/Create` keeps its picker: 0 steppers and `.bon-picker-item` still `padding: 12px 0`,
      `min-height: 44px`, so the shared partial did not leak the row's new shape.
- [x] Computed style matches the export: sheet `16px 16px 0 0` / `16px 20px 24px`, row `12px 0` padding
      with a `10px` gap and a 60px height, stepper 88x36 with an `8px` radius and a 1px `#E5E5E5`
      outline, 30px buttons (minus `#5C5C5C`, plus `#000000`), 24px value at 15px/600, name 15px/600,
      price Fraunces 16px/600. The stepper's right edge sits on the row's right edge (370px) — no overflow.
- [x] `POST /Bon/AddItem` with `Jumlah=3` wrote one line with `jumlah = 3`, painted
      **kopi ditambahkan ke bon**, `Rp 20.000 × 3 sachet`, subtotal `Rp 60.000`, and moved Total belanja
      from `Rp 3.500` to `Rp 63.500`. The test line was deleted again, so the notebook still holds the
      rows it started with.
- [x] `Jumlah=0` was refused with **Jumlah minimal 1 dan maksimal 9999** and wrote nothing.
- [ ] A human visual pass over `/Bon/Details` for pixel spacing against the export frame.

## Non-goals

Editing or deleting a line already in the bon, changing a recorded price, stock movement, a running
subtotal per row inside the sheet, server-side quantity validation beyond the request range, a second JS
library, and a SPA-style partial repaint (the redirect keeps one source of truth).
