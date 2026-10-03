# CatatHutang Detail Catat Bon slicing

Date: 2026-10-03  
Source: `d:\coding\MyBon\MyBon\CatatHutang-export.html` (screen `data-pencil-name="Detail Catat Bon"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml`, Bon list in `Views/Bon/Index.cshtml`, Bon form tokens in `wwwroot/css/site.css`

Design read: mobile read-only bon sheet for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform rows, one focal card, focus only on real controls).

## Goal

Add a static visual slice of one bon (`BN-014` for **Siti Aminah**): header back to the list, bon meta, a **Ringkasan** card (total belanja / total bayar / sisa hutang), the **Catat pembayaran** and **Tambah barang** actions, the **Riwayat pembayaran** list, and the **Daftar barang** list with a total footer. No database. Every Bon list card opens this same page.

## Constraints (agreed)

- Visual only: the page never persists anything. `Catat pembayaran` and `Tambah barang` are real anchors, but neither writes data.
- All four list cards are links to the same Details URL. Clicking BN-013 (or any other bon) still shows BN-014.
- Optional back in `_Layout` when `ViewData["BackHref"]` is set. On this page it is `/Bon`.
- Money uses Fraunces; labels, names, and dates stay Figtree, matching the export and the rest of the app.
- **Ringkasan** rows, history row, and item rows are read-only text, not inputs. Values come from the export mockup, not live shop data.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths. Icons: `bi-plus` (Tambah barang). Back and other chrome stay Bootstrap Icons.
- Do not render the mockup iPhone status bar.
- Do not duplicate the header inside the page. Layout wordmark stays **MY BON**.
- Out of scope: database, per-card data, real payment, real add-line, edit/delete, server validation, Tailwind, Phosphor paths from the export.

## Architecture

Shell stays shared. Details is a third action on the same controller so the Catat Bon tab stays active.

```
_Layout.cshtml                  optional back via ViewData["BackHref"] (unchanged)
BonController
  Index()                       list; each card links to Details
  Create()                      form (unchanged)
  Details()                     read-only view (GET)
Views/Bon/Index.cshtml          cards become anchors
Views/Bon/Details.cshtml        sheet markup
wwwroot/css/site.css            card-as-link and detail-sheet classes
```

URL: `/Bon/Details`.  
`ViewData["Title"]` = `BN-014`.  
`ViewData["BackHref"]` = `/Bon` (or `Url.Action` to Index).  
No ViewModels, services, or database. No query string. The controller ignores which card was clicked. `Index()` and `Create()` stay unchanged.

## Header (`_Layout.cshtml`)

Reuse the existing optional back. Details sets `BackHref` like Pelanggan/Details and Bon/Create. The shared header shows the wordmark **MY BON** and the screen title **BN-014**. That is the same shell choice the Pelanggan detail slice made, kept so all secondary screens share one voice; the export's inverted shop/title split is not reproduced.

## List change (`Views/Bon/Index.cshtml`)

Replace each `<div class="bon-card">` with:

```html
<a class="bon-card" asp-controller="Bon" asp-action="Details">
    ...
</a>
```

Keep the same inner markup (top row, name, date, amounts). Search stays visual-only. **Catat bon baru** still goes to `Bon/Create`.

## Page content (`Views/Bon/Details.cshtml`)

```html
@{
    ViewData["Title"] = "BN-014";
    ViewData["BackHref"] = Url.Action("Index", "Bon");
}
```

Then, in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. **Bon meta**: `Siti Aminah` (Figtree 16px semibold, `#000000`) above `12 Maret 2026 · Belum lunas` (Figtree 13px, `#5C5C5C`)
3. **Ringkasan** card: white, 1px `#E5E5E5` outline, 10px radius, 14px padding, column gap 10px:
   - Title `Ringkasan` (Figtree 12px, `#5C5C5C`)
   - Row `Total belanja` / `Rp 140.000` (label Figtree 14px `#000000`, value Fraunces 16px semibold)
   - Row `Total bayar` / `Rp 50.000`
   - Rule
   - Row `Sisa hutang` / `Rp 90.000` (value Fraunces 20px semibold, the focal figure)
4. **Actions** row, two equal-width 48px / 8px-radius anchors, gap 12px:
   - **Catat pembayaran**: `<a>` to `Bon/Index`, background `#000000`, white Figtree semibold 15px
   - **Tambah barang**: `<a>` to `Bon/Create`, background `#1877F2`, `bi-plus` + white Figtree semibold 15px
5. **Riwayat pembayaran**: section title Figtree 15px semibold; one row (padding 12px 0, space-between): left column **Cicilan 1** (Figtree 15px semibold) over `12 Maret 2026` (Figtree 12px `#5C5C5C`), right `Rp 50.000` Fraunces 16px semibold; rule under the row
6. **Daftar barang**: section title Figtree 15px semibold; three item rows (column gap 6px, padding 14px 0):
   - Name Figtree 15px semibold
   - Rinci Figtree 13px `#5C5C5C` (`Rp 78.000 × 1 karung` / `Rp 34.000 × 1 botol` / `Rp 28.000 × 1 kg`)
   - Bottom row, space-between: date Figtree 12px `#5C5C5C` / subtotal Fraunces 16px semibold
   - Rule after each item
   - **Total belanja** footer row (padding 14px 0, space-between): label Figtree 15px semibold; value `Rp 140.000` Fraunces 18px semibold

`Catat pembayaran` links to the list because no payment screen exists yet (same fallback the Pelanggan detail slice uses). `Tambah barang` links to `Bon/Create` because the bon form is the real home for adding goods, so neither control is dead.

## Styling

Add Bon-detail classes in `site.css` (for example `.bon-detail-page`, `.bon-detail-meta`, `.bon-summary`, `.bon-summary-row`, `.bon-action`, `.bon-history*`, `.bon-item*`, `.bon-item-total*`). Add `a.bon-card` for the list card-as-link, mirroring `a.pelanggan-card`.

Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Reuse `.page-rule`. Money uses Fraunces because the export does; labels and names stay Figtree.

Keep Bootstrap CSS/JS as already referenced. Do not restyle Dashboard, Barang, Pelanggan, Privacy, or Error.

Decision reasons (R-31): one bordered **Ringkasan** card makes the money the focal point, the same device the Bon form uses for its line card; Fraunces only on money so amounts scan like the list; the two actions are the only filled controls, so the accent stays at the key moment; item rules mirror the export's ledger rhythm without inventing chrome.

## Data and errors

No ViewModels. `Details()` returns `View()` with no arguments.

The page always shows the BN-014 mockup. Do not add empty, loading, or error UI on this page. Existing `HomeController.Error` is unchanged.

Mockup names, dates, and amounts are design fixtures, not live bon data.

## Verification

With `dotnet watch run`:

1. `/Bon`: each of the four cards opens `/Bon/Details`. Cards keep their chrome (not blue URL text).
2. `/Bon/Details`: header title **BN-014**, back chevron visible, Catat Bon tab active (`#1877F2`). Meta reads **Siti Aminah** / **12 Maret 2026 · Belum lunas**.
3. Ringkasan reads Total belanja `Rp 140.000`, Total bayar `Rp 50.000`, Sisa hutang `Rp 90.000` (the largest figure).
4. **Catat pembayaran** goes to `/Bon`; **Tambah barang** goes to `/Bon/Create`.
5. Riwayat shows **Cicilan 1** / `12 Maret 2026` / `Rp 50.000`. Daftar barang shows Beras Ramos 5 kg, Minyak Goreng 2 L, Telur Ayam with dates/subtotals and a total footer `Rp 140.000`.
6. Back, and both actions, return to a real page. No dead controls.
7. Keyboard: Tab lands on back, Catat pembayaran, Tambah barang, then the shell tabs. Focus ring visible. Desktop: narrow centered shell; mobile: content scrolls above the bottom nav.

## Non-goals

Login, database, different detail per bon, query-string binding, real payment, real add-line, edit form, confirm-delete, Tailwind utilities from the export, pixel-perfect Phosphor icons.