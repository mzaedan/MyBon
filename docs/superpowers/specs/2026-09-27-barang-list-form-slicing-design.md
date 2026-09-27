# CatatHutang Barang list + form slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Barang"`) and `d:\coding\Design\CatatHutang.pen` (frame `Tambah Barang`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml` (header MY BON + title, optional back via `ViewData["BackHref"]`, four-tab bottom nav)

Design read: mobile stock list and write-item form for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform cards and fields, focus only).

## Goal

Replace the Barang placeholder with a static list slice (search row, count, eight item cards, **Tulis barang baru**) and add a visual write-item form at `/Barang/Create`. No database.

## Constraints (agreed)

- Approach: mirror Pelanggan (Index list + Create form), not shared partials, not a query-string toggle on one URL.
- List search is visual only: icon + text **Cari barang** + rule. Not an `<input>`, not a `<form>`, no filtering.
- Cards are `div`s: no `href`, no detail or edit page.
- **Tulis barang baru** is a real link to `Barang/Create`.
- Form is visual only: **Simpan barang** does not persist. GET submit re-renders Create with empty fields.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths. Search uses Bootstrap Icons `bi-search`. Back uses existing `bi-chevron-left` in `_Layout`.
- Do not render the mockup iPhone status bar.
- Do not duplicate the header inside the page. Layout wordmark stays **MY BON** (ignore "Toko Kelontong Berkah" on the pen frame).
- Out of scope: login, database, live search, item detail, edit/delete, server validation, Tailwind, Phosphor paths from the export.

## Architecture

Shell stays shared. Create is a second action on `BarangController` so the Barang tab stays active.

```
_Layout.cshtml                 unchanged (back already exists)
BarangController
  Index()                      list; CTA links to Create
  Create()                     form view (GET)
Views/Barang/Index.cshtml      list markup
Views/Barang/Create.cshtml     form markup
wwwroot/css/site.css           barang list classes; form reuses pelanggan form classes
```

URL: `/Barang` and `/Barang/Create`.  
Index: `ViewData["Title"]` = `Barang`.  
Create: `ViewData["Title"]` = `Barang baru`; `ViewData["BackHref"]` = `Url.Action("Index", "Barang")`.  
No ViewModels, services, or database. Do not change Pelanggan, Dashboard, Bon, Privacy, or Error views.

## List (`Views/Barang/Index.cshtml`)

```html
@{
    ViewData["Title"] = "Barang";
}
```

Static markup in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. Search row: `bi-search` (18px, `#5C5C5C`) + **Cari barang** (Figtree 15px, `#5C5C5C`) + rule. Mark the row `aria-hidden="true"` like Pelanggan.
3. List title **8 barang di buku** (Figtree 13px semibold, `#5C5C5C`). Count is hardcoded.
4. Eight cards, gap 8px. Each card is a `div`: white, 1px outline `#E5E5E5`, radius 10px, padding 14px. Inner layout is a row, `justify-content: space-between`, `align-items: center`, gap 12px:
   - Left: name (Figtree 15px semibold, `#000000`) then satuan (Figtree 13px, `#5C5C5C`), inner gap 4px
   - Right: price (Fraunces 16px semibold, `#000000`), nowrap
5. Full-width **Tulis barang baru** as `<a class="cta-catat-bon" asp-controller="Barang" asp-action="Create">`. Same class and metrics as Pelanggan **Tulis nama baru** (48px height, `#1877F2`, 8px radius, white Figtree semibold 16px).

The list scrolls in `main`. The CTA sits after the cards in document order, not sticky above the nav. Do not add **Muat berikutnya**: the mockup has eight rows and no load-more control.

### Hardcoded rows (from the export mockup, not live shop data)

| Name | Satuan | Price |
|---|---|---|
| Beras Ramos 5 kg | karung | Rp 78.000 |
| Minyak Goreng 2 L | botol | Rp 34.000 |
| Gula Pasir | kg | Rp 17.500 |
| Telur Ayam | kg | Rp 28.000 |
| Mie Instan | dus | Rp 125.000 |
| Kopi Sachet | renceng | Rp 12.000 |
| Sabun Mandi | pcs | Rp 4.500 |
| Garam Dapur | bungkus | Rp 3.000 |

No avatars, no extra columns.

## Form (`Views/Barang/Create.cshtml`)

```html
@{
    ViewData["Title"] = "Barang baru";
    ViewData["BackHref"] = Url.Action("Index", "Barang");
}
```

Then, in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. `<form method="get" asp-action="Create" class="pelanggan-form">` (reuse existing form classes; do not invent a second field system)
3. Field **Nama Barang**: label 12px `#5C5C5C`, `<input type="text" name="namaBarang" placeholder="Tulis nama barang" autocomplete="off">`, rule
4. Field **Satuan**: label, `<input type="text" name="satuan" placeholder="Tulis satuan" autocomplete="off">`, rule
5. Field **Harga**: label, `<input type="text" name="harga" inputmode="decimal" placeholder="Tulis harga" autocomplete="off">`, rule
6. Actions row, two equal-width 48px / 8px-radius controls (existing `.pelanggan-form-actions` / `.pelanggan-btn*`):
   - **Batal**: `<a>` to `Barang/Index`, background `#000000`, white Figtree semibold 16px
   - **Simpan barang**: `<button type="submit">`, background `#1877F2`, white Figtree semibold 16px

Empty values. No fake product names in the fields. GET submit re-renders Create; query string may appear; ignore it, do not bind or store. That is the only Simpan behavior.

**Batal** and header back both go to `Barang/Index`.

## Styling

Add Barang-list classes in `site.css` (for example `.barang-page`, `.barang-search`, `.barang-card`, `.barang-card-price`). Search row metrics match Pelanggan search; group selectors if that avoids duplicated rules. Do not restyle Pelanggan cards to look like Barang cards.

Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Reuse `.page-rule` and `a.cta-catat-bon`. Price uses Fraunces because the export does; names and satuan stay Figtree.

Keep Bootstrap CSS/JS as already referenced.

## Data and errors

No ViewModels. `Index()` and `Create()` return `View()` with no arguments.

List always shows the eight mockup rows. Form always shows the empty form. Do not add empty, loading, or error UI on these two pages. Existing `HomeController.Error` is unchanged.

## Verification

With `dotnet watch run`:

1. `/Barang`: title **Barang**, Barang tab active (`#1877F2`), search text **Cari barang**, line **8 barang di buku**, eight cards with name / satuan / `Rp` price, **Tulis barang baru** opens `/Barang/Create`.
2. `/Barang/Create`: title **Barang baru**, back chevron visible, Barang tab still active. Back and **Batal** return to `/Barang`. `/Barang` itself has no back chevron.
3. Placeholders read Tulis nama barang / Tulis satuan / Tulis harga. Typing works. **Simpan barang** does not add a card; it returns to the empty form.
4. Cards are not links. Search is not an input.
5. Keyboard: Tab through Create back, fields, Batal, Simpan. Focus ring visible. Desktop: narrow centered shell; mobile: list and form scroll above the bottom nav.

## Non-goals

Login, database, validation messages, prefilled query binding, item detail, edit/delete, live search, load-more, Tailwind utilities from the export, pixel-perfect Phosphor icons.
