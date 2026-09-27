# CatatHutang Catat Bon form slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Catat Bon Baru"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml` (header MY BON + title, optional back via `ViewData["BackHref"]`, four-tab bottom nav); Catat Bon list at `/Bon`

Design read: mobile write-bon form for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform underline fields and one line card, focus only).

## Goal

Add a visual Catat Bon create form at `/Bon/Create` matching the export, and turn **Catat bon baru** on the list into a real link. No database. No automatic totals. No add/remove line rows.

## Constraints (agreed)

- Approach: mirror Barang (Index list + Create form). Reuse `.pelanggan-form` field classes for the top fields. New classes only for the line card, daftar header, qty box, and total belanja row. No shared partials. No query-string toggle on `/Bon`.
- Pelanggan, tanggal, and pilih barang are native `select` / `input type="date"` with dummy options. Jumlah is a native number input. Catatan is a textarea. Total Bayar is a text input (`inputmode="decimal"`). They do not persist.
- Harga, satuan, subtotal, and total belanja are display-only (`Rp 0` / `-`). They do not update when barang or jumlah changes.
- **Tambah barang** and **Hapus** are visual `div`s: `aria-hidden="true"`, not in the tab order, no click behavior.
- **Simpan catatan** does not persist. GET submit re-renders Create with empty fields.
- **Catat bon baru** on Index is `<a class="cta-catat-bon">` to `Bon/Create`. Remove the visual-only `.bon-cta` usage (and its CSS if unused).
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths. Icons: `bi-chevron-down`, `bi-calendar`, `bi-plus`, `bi-trash`. Back uses existing `bi-chevron-left` in `_Layout`.
- Do not render the mockup iPhone status bar.
- Do not duplicate the header inside the page. Layout wordmark stays **MY BON**.
- Out of scope: login, database, live search, bon detail, edit/delete, add/remove lines, calculated totals, custom pickers, server validation, Tailwind, Phosphor paths from the export.

## Architecture

Shell stays shared. Create is a second action on `BonController` so the Catat Bon tab stays active.

```
_Layout.cshtml                 unchanged (back already exists)
BonController
  Index()                      list; CTA links to Create
  Create()                     form view (GET)
Views/Bon/Index.cshtml         list markup; CTA becomes a real link
Views/Bon/Create.cshtml        form markup
wwwroot/css/site.css           bon line-card classes; select/date tweaks; drop unused .bon-cta
```

URL: `/Bon` and `/Bon/Create`.  
Index: `ViewData["Title"]` = `Catat Bon`. Do not set `BackHref`.  
Create: `ViewData["Title"]` = `Bon baru`; `ViewData["BackHref"]` = `Url.Action("Index", "Bon")`.  
No ViewModels, services, or database. Do not change Pelanggan, Barang, Dashboard, Privacy, or Error views except that Bon Index CTA becomes a link.

## List CTA (`Views/Bon/Index.cshtml`)

Replace:

```html
<div class="bon-cta" aria-hidden="true">Catat bon baru</div>
```

with:

```html
<a class="cta-catat-bon" asp-controller="Bon" asp-action="Create">Catat bon baru</a>
```

Same metrics as Pelanggan **Tulis nama baru** and Barang **Tulis barang baru** (48px height, `#1877F2`, 8px radius, white Figtree semibold 16px). Real pointer, `:active`, focus ring. Cards and search stay visual-only.

## Form (`Views/Bon/Create.cshtml`)

```html
@{
    ViewData["Title"] = "Bon baru";
    ViewData["BackHref"] = Url.Action("Index", "Bon");
}
```

Then, in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. `<form method="get" asp-action="Create" class="pelanggan-form">`
3. Field **Pelanggan**: label 12px `#5C5C5C`, `<select id="pelanggan" name="pelanggan" class="pelanggan-field-input">`. First option value empty, visible text **Pilih pelanggan**. Then four dummy names from the Pelanggan/Bon lists:
   - Siti Aminah
   - Budi Santoso
   - Rina Wahyuni
   - Agus Salim  
   Caret: `bi-chevron-down` (16px, `#5C5C5C`), not a second interactive control. Rule under the field.
4. Field **Tanggal**: label, `<input id="tanggal" type="date" name="tanggal" class="pelanggan-field-input" value="">`. Calendar icon `bi-calendar` (16px, `#5C5C5C`) is visual, `aria-hidden="true"`, pointer-events none so the date input remains the control. Rule.
5. Field **Total Bayar**: label, `<input id="totalBayar" type="text" name="totalBayar" inputmode="decimal" class="pelanggan-field-input bon-money-input" placeholder="Tulis total bayar" autocomplete="off" value="">`. Value and placeholder use Fraunces semibold 16px. Rule.
6. Field **Catatan**: label, `<textarea id="catatan" name="catatan" class="pelanggan-field-input pelanggan-field-textarea" placeholder="Tulis catatan" rows="2"></textarea>`. Rule.
7. **Daftar barang** row: title Figtree 15px semibold `#000000` on the left. Right: visual **Tambah barang** (`div`, `aria-hidden="true"`, height 44px): `bi-plus` 16px + label Figtree 14px semibold `#000000`. Not a link or button.
8. One line card `.bon-line`: white, 1px outline `#E5E5E5`, radius 10px, padding 12px, inner column gap 10px:
   - Barang row: `<select id="barang" name="barang" class="pelanggan-field-input">`. First option **Pilih barang** (empty value). Then the eight Barang list names: Beras Ramos 5 kg, Minyak Goreng 2 L, Gula Pasir, Telur Ayam, Mie Instan, Kopi Sachet, Sabun Mandi, Garam Dapur. Caret `bi-chevron-down`. Rule inside the card.
   - Metrics row, three columns (harga flex, jumlah shrink, satuan flex), gap 8px:
     - **Harga** label 12px muted, value `Rp 0` Fraunces semibold 16px, display only
     - **Jumlah** label, `<input type="number" name="jumlah" min="1" value="1" class="bon-line-qty">` in a box padding 8px 12px, outline `#E5E5E5`, radius 6px, Figtree 16px
     - **Satuan** label, value `-` Figtree 16px `#000000`, display only
   - Subtotal row, space-between: **Subtotal** label + `Rp 0` Fraunces semibold 16px; visual hapus `div` 44×44, `bi-trash` 20px, `aria-hidden="true"`
9. **Total belanja** row, padding 12px 0, space-between: label Figtree 15px semibold; value `Rp 0` Fraunces 18px semibold. Display only.
10. Actions row, two equal-width 48px / 8px-radius controls (`.pelanggan-form-actions` / `.pelanggan-btn*`):
    - **Batal**: `<a>` to `Bon/Index`, background `#000000`, white Figtree semibold 16px
    - **Simpan catatan**: `<button type="submit">`, background `#1877F2`, white Figtree semibold 16px

Empty values (jumlah is `1`). No fake amounts or notes in the fields. GET submit re-renders Create; query string may appear; ignore it, do not bind or store. That is the only Simpan behavior.

**Batal** and header back both go to `Bon/Index`.

Native `<select>` and `input[type="date"]` keep browser chrome for the picker UI. Page chrome (underline, caret, calendar glyph) is restyled to match Pelanggan fields: no extra box border, transparent background.

## Styling

Add Bon-form classes in `site.css` (for example `.bon-field-with-icon` for select/date row, `.bon-money-input`, `.bon-daftar-head`, `.bon-tambah`, `.bon-line`, `.bon-line-metrics`, `.bon-line-qty`, `.bon-line-subtotal`, `.bon-total-row`, `.bon-total-value`). Group with existing field selectors when metrics match. Do not restyle Pelanggan or Barang forms to look like the line card.

Remove `.bon-cta` from `site.css` once Index no longer uses it.

Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Reuse `.page-rule`, `.pelanggan-form`, `.pelanggan-field*`, `.pelanggan-form-actions`, `.pelanggan-btn*`, `a.cta-catat-bon`. Money uses Fraunces because the export does; labels and names stay Figtree.

Keep Bootstrap CSS/JS as already referenced.

Decision reasons (R-31): underline fields match Pelanggan so write screens share one voice; Fraunces only on money so amounts scan like the list; one line card with outline so the barang block is the page focal point; visual Tambah/Hapus stay on-screen to match the export without dead buttons.

## Data and errors

No ViewModels. `Index()` and `Create()` return `View()` with no arguments.

List always shows the four mockup rows. Form always shows the empty form (jumlah `1`, money displays `Rp 0`). Do not add empty, loading, or error UI on these two pages. Existing `HomeController.Error` is unchanged.

## Verification

With `dotnet watch run`:

1. `/Bon`: title **Catat Bon**, Catat Bon tab active (`#1877F2`), **Catat bon baru** opens `/Bon/Create`. Cards and search still do not navigate. `/Bon` has no back chevron.
2. `/Bon/Create`: title **Bon baru**, back chevron visible, Catat Bon tab still active. Back and **Batal** return to `/Bon`.
3. Placeholders: Pilih pelanggan, empty date, Tulis total bayar, Tulis catatan, Pilih barang. Jumlah starts at `1`. Harga, subtotal, total belanja stay `Rp 0`. Satuan stays `-` after choosing a barang.
4. Typing and native pickers work. **Simpan catatan** does not add a list card; it returns to the empty form.
5. **Tambah barang** and **Hapus** do not change the DOM and are not in the tab order.
6. Keyboard: Tab through Create back, selects, date, total bayar, catatan, jumlah, Batal, Simpan. Focus ring visible. Desktop: narrow centered shell; mobile: form scrolls above the bottom nav.

## Non-goals

Login, database, validation messages, prefilled query binding, bon detail, edit/delete, live search, add/remove line rows, calculated harga/subtotal/total, custom dropdowns, Tailwind utilities from the export, pixel-perfect Phosphor icons.
