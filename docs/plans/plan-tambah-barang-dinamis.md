# Tambah barang dinamis plan

**Goal:** On `/Bon/Details`, the **Tambah barang** sheet lists the real stock list and one tap writes the line into the open bon, so Ringkasan, Daftar barang, and Total belanja change with the notebook. The sheet shipped as eight hardcoded rows in `docs/plans/plan-tambah-barang-slicing.md`; they were the last static data on the page.

**Depends on:** `Views/Bon/Details.cshtml`, `Views/Bon/_BarangRows.cshtml`, `BonService.SearchBarangAsync` (both added for the Create picker by `docs/plans/plan-bon-dinamis.md`, Slice 5), tokens in `wwwroot/css/site.css`.

**Status:** implemented and verified against Postgres on 2026-10-31. The sheet paints the first page of stock `/Bon/Details/{id}` already carries, searches and pages through `/Bon/SearchBarang`, and `POST /Bon/AddItem` writes one unit per tap before redirecting back to the page. Recording a payment is still visual (`docs/plans/plan-catat-pembayaran-slicing.md`).

## Architecture

```
GET  /Bon/Details/{id}         BonService.GetDetailAsync        -> BonDetailDto + first barang page
                               Views/Bon/Details.cshtml         -> <partial _BarangRows>, #bonAddForm
GET  /Bon/SearchBarang?q&page  BonService.SearchBarangAsync     -> _BarangRows (shared with Create)
POST /Bon/AddItem              BonService.AddItemAsync          -> bon_item row -> redirect Details
```

One tap writes one unit. The price is never posted: the service copies the name, unit, and price from
`barang` at the moment the line is written, exactly like `CreateAsync`, so changing a master price
later never rewrites an old page. Totals stay derived (`SUM(harga_satuan * jumlah)`), so the redirect
repaints the summary, the item list, and the pay sheet's sisa hint from one source.

## File map

- Create: `Models/Bon/Requests/AddBonItemRequest.cs`
- Modify: `Models/Bon/DTOs/BonDetailDto.cs` (`Barangs` first page)
- Modify: `Domain/Interfaces/IBonService.cs`, `Domain/Services/BonService.cs` (`AddItemAsync`, detail page rows)
- Modify: `Controllers/BonController.cs` (`AddItem` POST, `TempData` status/error)
- Modify: `Views/Bon/Details.cshtml` (notice banner, `#bonAddForm`, `_BarangRows`, `data-search-url`)
- Modify: `wwwroot/custom/features/bon/details.js` (server search, load-more, pick submits the form)
- Modify: `wwwroot/css/site.css` (`.bon-add-form` keeps the list scrolling, `.bon-notice--error`)
- Modify: `docs/plans/plan-tambah-barang-slicing.md`, `docs/plans/plan-bon-dinamis.md` (this plan supersedes their "writes nothing" notes)

## Slice 1: data

- [x] **Step 1:** `GetDetailAsync` also returns `ReadBarangPageAsync(null, 1)`, so the sheet paints real
  rows without the page carrying the whole stock list.
- [x] **Step 2:** `AddBonItemRequest` posts `BonId` and `BarangId` only; `BarangId` is required.
- [x] **Step 3:** `AddItemAsync` refuses a missing bon with `ErrorType.NotFound`, refuses a missing
  barang with a `BarangId` validation error, and writes one `bon_item` row: name, unit, and price
  copied from the stock list, `jumlah = 1`, `tanggal = today`.

## Slice 2: write path

- [x] **Step 1:** `POST /Bon/AddItem` is anti-forgery protected and redirects back to
  `Details/{id}`; a rejected pick lands on the same page with `TempData["BonError"]` instead of a bare
  status code.
- [x] **Step 2:** A success lands with `TempData["BonStatus"]` (**<nama> ditambahkan ke bon**); a bon
  that no longer exists sends the shopkeeper to `/Bon`.
- [x] **Step 3:** Field messages win over the generic `ErrorOr` message, so an unknown barang reads
  **Barang tidak ditemukan**.

## Slice 3: sheet

- [x] **Step 1:** `#bonAddSheet` carries `data-search-url="/Bon/SearchBarang"`; the eight hardcoded
  `.bon-add-row` blocks became `<partial name="_BarangRows" model="Model.Barangs" />`.
- [x] **Step 2:** `#bonAddForm` holds `BonId` (the page) and `BarangId` (filled by the pick), with the
  anti-forgery token the form tag helper writes.
- [x] **Step 3:** `details.js` debounces the search box (250 ms), aborts the in-flight request, replaces
  only the rows so the box keeps focus, appends the next page through the button the server renders,
  and clears the term when the sheet closes.
- [x] **Step 4:** Picking a `.bon-picker-item` fills `BarangId` and submits the form once; a second tap
  before the POST lands is ignored.
- [x] **Step 5:** `.bon-add-form` takes the sheet's growing row, so `.bon-add-list` keeps its own scroll.

## Verification (Postgres + headless Chrome, 2026-10-31)

- [x] `dotnet build` succeeds: 0 warnings, 0 errors. `node --check details.js` passes.
- [x] `/Bon/Details/14` painted eleven real stock rows in the sheet, `#bonAddForm` carrying `BonId=14`,
  and an anti-forgery token.
- [x] Typing **kopi** painted **kopi**, **kopi gooday**, and **kopi sachet**; closing the sheet cleared
  the box and repainted all eleven rows.
- [x] Tapping **bawang putih** (`Rp 500`) posted the line, redirected to `/Bon/Details/14`, showed
  **bawang putih ditambahkan ke bon**, moved Total belanja from `Rp 24.500` to `Rp 25.000`, and added
  the line to Daftar barang.
- [x] A POST without `BarangId` showed **Barang wajib dipilih.**; `BarangId=9999` showed **Barang tidak
  ditemukan**; `BonId=9999` redirected to `/Bon`. No line was written by any rejected pick.
- [x] The two test lines were deleted again, so the notebook still held the rows it started with.

## Non-goals

Editing the quantity in the sheet, deleting a line, changing a recorded price, stock movement, a
second JS library, SPA-style partial repaint (the redirect keeps one source of truth), and payment
persistence.
