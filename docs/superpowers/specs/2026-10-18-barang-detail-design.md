# CatatHutang Barang detail design

Date: 2026-10-18  
App: ASP.NET Core MVC MyBon  
Depends on: Barang list + edit slices (`Views/Barang/Index.cshtml`, `Views/Barang/Edit.cshtml`), `IBarangService.GetByIdAsync`, the Pelanggan detail flow (`PelangganController.Details`, `Views/Pelanggan/Details.cshtml`), `BaseController`

Design read: mobile read-only item sheet for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform fields, one action, the price as the only loud number).

Extends `2026-10-18-barang-edit-design.md` and supersedes its **item detail page** non-goal, and the same non-goal in `2026-10-18-barang-empty-state-design.md` and `2026-10-18-barang-create-persist-design.md`.

## Goal

Give `/Barang` a read step that matches the Pelanggan flow: tapping an item card opens `/Barang/Details/{id}` with the item's name, unit, and price, and **Ubah barang** on that sheet opens the existing edit form.

## Constraints (agreed)

- Mirror Pelanggan Details: read-only labels and values, the entity as the model, no ViewModel, no service change. `GetByIdAsync` already exists for Edit and serves this screen unchanged.
- The card on `/Barang` opens Details; the sheet, not the card, holds the edit action, so the list matches the Pelanggan list (card opens read view, read view holds the writes).
- One action only. `IBarangService` has no delete and the item write set has no other destination, so the sheet does not grow a second button that would do nothing.
- The price prints the way the list prints it (`Rp 78.000`) and wears the display face at 20px, one step above the list's 16px, because it is the one number the sheet exists to show.
- Reuse the `.pelanggan-form*` and `.pelanggan-field*` classes exactly as the Barang forms already do. One new class for the price.
- Out of scope: item delete, live search on `/Barang`, stock count, item photo, barcode, `UpdatedAt`, Bon history for the item.

## Architecture

```
Controllers/BarangController.cs   Details(int id) GET
Views/Barang/Details.cshtml       read-only fields + Ubah barang
Views/Barang/Index.cshtml         a.barang-card -> Details
wwwroot/css/site.css              .barang-detail-price
```

`Details` returns `ToActionResult(result, barang => View(barang))`, so a missing row is a 404, an AJAX call gets the `ErrorOr` payload as JSON, and a page request renders the sheet.

| Field | Value |
|---|---|
| Nama barang | `barang.Nama` |
| Satuan | `barang.Satuan`, or the `—` glyph Pelanggan Details already uses for an empty value |
| Harga | `Rp` plus thousands-separated `barang.Harga`, same formatter as the list |

Header: `ViewData["Title"]` is the item name, `ViewData["BackHref"]` is `/Barang`.

## Behaviour

1. `/Barang` lists items as cards; tapping one opens `/Barang/Details/{id}`.
2. The sheet is read-only. **Ubah barang** opens `/Barang/Edit/{id}`; the back chevron and the Barang tab still work as everywhere else.
3. An unknown id is a 404 through `ToActionResult`. No form, no token, nothing to post.
4. The confirmation notice stays on the list: the sheet is a look, not a write, so `TempData["BarangStatus"]` is not read here.

## Styling

The sheet is `.pelanggan-form` with three `.pelanggan-field` blocks, each closed by a `.page-rule` line, exactly like Pelanggan Details. The single action reuses `.pelanggan-form-actions` and `.pelanggan-btn .pelanggan-btn-simpan`, so it fills the row at 48px and stays the accent-blue action.

`.barang-detail-price` sets `font-family: var(--font-display)`, `font-size: 20px`, `font-weight: 600` on top of `.pelanggan-field-value`. Nothing else is added: no new palette, no new radius, no motion.

## Data and errors

- `GetByIdAsync` reads with `AsNoTracking` and returns **Barang tidak ditemukan** as `ErrorType.NotFound` for a missing row.
- No new query, no new service method, no migration.
- A `Satuan` of null or whitespace reads as the empty-value glyph, the same as an unfilled Pelanggan field.

## Reasons (R-31)

- **Why the item now gets a read step**: the Pelanggan list and the Barang list are the same gesture (tap a card), so they now answer the same way; the write moved one tap in, where the sheet names the item it acts on.
- **Why one button**: a delete button would have no service to call, and a control that cannot act is worse than no control.
- **Why the price is the largest text**: the sheet answers "how much is this item", so the answer is the focal point and the two labels stay at label size.
- **Why the empty unit keeps the `—` glyph**: two detail sheets in one notebook should say "not filled" the same way.

## Verification

With `dotnet watch run` and the Postgres container up:

1. `/Barang` with an item: tapping a card opens `/Barang/Details/{id}`. Header title is the item name, back chevron visible, Barang tab active.
2. The sheet shows the name, the unit (or `—` when the item has none), and a price reading `Rp 78.000`, matching the card.
3. **Ubah barang** opens `/Barang/Edit/{id}` with the item's values filled in.
4. `/Barang/Details/99999` is a 404. An AJAX GET of the same URL returns the JSON payload instead of a page.
5. Keyboard: Tab reaches the card on the list and **Ubah barang** on the sheet, with a visible focus ring. Desktop: narrow centred shell; mobile: content clears the bottom nav.

## Non-goals

Item delete, live search on `/Barang`, stock or quantity, item photo, barcode, price history, `UpdatedAt`, Bon lines for the item, printing or sharing the sheet.