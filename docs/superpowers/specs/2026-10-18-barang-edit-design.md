# CatatHutang Barang edit design

Date: 2026-10-18  
App: ASP.NET Core MVC MyBon  
Depends on: Barang list + create slices (`Views/Barang/Index.cshtml`, `Views/Barang/Create.cshtml`), `IBarangService`, `CreateBarangRequest`, the Pelanggan edit flow (`PelangganController.Edit`, `PelangganService.UpdateAsync`, `UpdatePelangganRequest`), `BaseController`

Design read: changing an already written item for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (the same field set as writing, one action, an error beside the field that caused it).

Extends `2026-10-18-barang-create-persist-design.md` and supersedes its **item edit** non-goal.

## Goal

Let a shopkeeper fix an item's name, unit, or price, so a mistyped price does not need a rewrite. The card on `/Barang` opens the edit form, **Simpan perubahan** writes the row, and the list returns with the confirmation line.

## Constraints (agreed)

- Mirror Pelanggan Edit: request model, service method, POST action bound to the route id, form re-rendered on failure. No second field system, no new form classes.
- Mirror Barang Create for the price: `Harga` stays text in the request and the service parses it, so `34.000` keeps meaning 34000 on any host culture.
- Create and edit post the same three fields, so they share one reader: `BarangService.ReadFields` trims, parses, and reports the same sentences (**Nama barang wajib diisi**, **Harga harus angka, contoh 78.000**, **Harga terlalu besar**).
- The price field opens with the amount written the way the list shows it (`34.000`, `1.234,5`), because the shopkeeper edits the number they read, not `34000.00`.
- The card is the way in. An item has three fields and the list already shows all three, so a read-only detail step would add a tap and nothing to read — it is out of scope, not a missing screen.
- Out of scope: item delete, item detail page, duplicate-name rules, live search on `/Barang`, soft delete, undo, audit trail.

## Architecture

```
Models/Barang/Requests/UpdateBarangRequest.cs   form input, data annotations, hidden Id
Domain/Interfaces/IBarangService.cs             GetByIdAsync, UpdateAsync
Domain/Services/BarangService.cs                ReadFields shared with create, load, assign, save
Controllers/BarangController.cs                 Edit() GET, Edit(int id, request) POST, FormatHarga
Views/Barang/Edit.cshtml                        POST form, asp-for fields, validation spans
Views/Barang/Index.cshtml                       a.barang-card -> Edit
wwwroot/custom/features/barang/harga-input.js   money formatting, shared with Create (renamed from create.js)
wwwroot/css/site.css                            a.barang-card link + focus ring
```

`UpdateAsync` returns `ErrorOr<Barang>`. Success goes through `ToActionResult` to `Index` with `TempData["BarangStatus"]`; failure re-enters the form through `AddErrorsToModelState`.

Field rules:

| Field | Rule |
|---|---|
| Id | hidden field, must equal the route id or the POST is a 400 |
| Nama | required, 200 max; annotation plus a whitespace check in the service |
| Satuan | optional, 30 max |
| Harga | required, 20 characters max, must parse and fit the column |

## Behaviour

1. `/Barang` lists items as cards; tapping one opens `/Barang/Edit/{id}` with the item's values filled in.
2. A valid POST writes the row, then redirects to `/Barang` with **Barang diperbarui di buku** above the list.
3. A failed POST re-renders the form with the typed values kept and the message under the field, exactly as Create does.
4. An unknown id is a 404 through `ToActionResult`; a posted Id that differs from the route id is a 400.
5. AJAX callers receive the `ErrorOr` payload as JSON instead of a page, per `BaseController`.
6. The form carries the anti-forgery token; a POST without one is a 400.

## Styling

`.barang-card` keeps its metrics; `a.barang-card` adds `color: inherit`, `text-decoration: none`, `cursor: pointer`, and the same `:focus-visible` ring `a.pelanggan-card` already uses. The form reuses the `.pelanggan-form*` classes unchanged, and the confirmation reuses `.barang-notice`.

## Data and errors

- `GetByIdAsync` reads with `AsNoTracking` and returns **Barang tidak ditemukan** as `ErrorType.NotFound` for a missing row.
- `UpdateAsync` loads the tracked row, reads the input, then assigns. Reading runs before the first assignment, so a rejected price leaves the tracked row untouched.
- A save failure that reaches the database (constraint, connection) logs it and returns **Gagal memperbarui barang. Coba simpan lagi.** as `ErrorType.Unexpected`. Validation never throws.
- No `UpdatedAt` column exists and none is added: the notebook has no history view to read it back.
- Reusing `TempData["BarangStatus"]` means the list keeps one notice slot for both writes.

## Reasons (R-31)

- **Why the price is prefilled with dots**: the list prints `Rp 34.000` and the field takes digits, so a plain `34000` would be re-typed from the screen anyway; showing the same shape keeps the round trip honest.
- **Why one shared reader for create and update**: a second validation path would sooner or later answer the same mistake with two different sentences.
- **Why the card links straight to the form**: every field of an item is already on the card, so a detail screen would only repeat the list in a bigger font.
- **Why the id comes from the route**: a hidden field is client-controlled, so comparing it with the route id keeps a tampered form from editing another row.
- **Why the money script was renamed**: `harga-input.js` now serves two screens, and the file name says what it does instead of which screen it started on.

## Verification

Run with `dotnet run` and the Postgres container up:

1. `/Barang` with an item: the card is a link, Tab reaches it as a whole, and the focus ring is visible.
2. Opening a card shows `/Barang/Edit/{id}`, title **Ubah barang**, back chevron, Barang tab active, and fields filled with the item's name, satuan, and a `34.000`-shaped price.
3. Changing the price to `35.500` and saving lands on `/Barang` with **Barang diperbarui di buku** and the card reading `Rp 35.500`.
4. `1.234,5` stores 1234.50. `dua ribu` re-renders with **Harga harus angka, contoh 78.000**; an empty name re-renders with **Nama barang wajib diisi**; `9999999999999` re-renders with **Harga terlalu besar**. After every rejected POST, `select nama, satuan, harga from barang` still shows the old row.
5. `/Barang/Edit/99999` is a 404. A POST whose hidden `Id` differs from the route id is a 400. A POST without `__RequestVerificationToken` is a 400.
6. Keyboard: Tab reaches every field, **Batal**, and **Simpan perubahan**.

## Non-goals

Item delete, item detail page, live search on `/Barang`, barcode or photo fields, duplicate detection, unit picker, audit trail, `UpdatedAt` column, soft delete, undo.

> Update: the item detail page now exists (`/Barang/Details/{id}`). The card opens that sheet and the sheet carries **Ubah barang** into the form described above; the edit flow itself is unchanged. See `2026-10-18-barang-detail-design.md`.
