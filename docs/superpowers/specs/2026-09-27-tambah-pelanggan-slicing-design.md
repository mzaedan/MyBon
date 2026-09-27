# CatatHutang Tambah Pelanggan slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Tambah Pelanggan"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml`, Pelanggan list slice in `Views/Pelanggan/Index.cshtml`

Design read: mobile form for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform fields, focus only).

## Goal

Add a static visual slice of the write-name screen: header back to the list, fields Nama / Nomor HP / Alamat, **Batal** and **Simpan pelanggan**. No database.

## Constraints (agreed)

- Visual only: Simpan does not persist. Same spirit as the list slice.
- Optional back in `_Layout` when `ViewData["BackHref"]` is set. On this page it is `/Pelanggan`.
- **Tulis nama baru** on the list becomes a real link to this page.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths; back icon is Bootstrap Icons `bi-chevron-left`.
- Real `<input>` / `<textarea>` with mockup placeholders. Empty values, no fake names.
- Do not render the mockup iPhone status bar.
- Do not change header type hierarchy on other pages.
- Out of scope: database, server validation, WhatsApp, edit/delete customer, Tailwind, Phosphor from the export.

## Architecture

Shell stays shared. Create is a second action on the same controller so the Pelanggan tab stays active.

```
_Layout.cshtml                 optional back via ViewData["BackHref"]
PelangganController
  Index()                      list; CTA links to Create
  Create()                     form view (GET)
Views/Pelanggan/Create.cshtml   form markup
wwwroot/css/site.css           form classes only
```

URL: `/Pelanggan/Create`.  
`ViewData["Title"]` = `Nama baru`.  
`ViewData["BackHref"]` = `/Pelanggan` (or `Url.Action` to Index).  
No ViewModels, services, or database.

## Header back (`_Layout.cshtml`)

When `ViewData["BackHref"]` is a non-empty string, show a 44px link before the header text:

- `href` = that value
- Icon `bi-chevron-left`, 22px, `#000000`
- Accessible name: **Kembali**
- Other pages omit `BackHref` and look as they do now

**Batal** also goes to `Pelanggan/Index`. Same destination as back.

## List change (`Views/Pelanggan/Index.cshtml`)

Replace `<button type="button" class="cta-catat-bon">Tulis nama baru</button>` with:

```html
<a class="cta-catat-bon" asp-controller="Pelanggan" asp-action="Create">Tulis nama baru</a>
```

Keep the same CTA class. Ensure the anchor layouts like the button (full width, 48px, centered label).

## Page content (`Views/Pelanggan/Create.cshtml`)

```html
@{
    ViewData["Title"] = "Nama baru";
    ViewData["BackHref"] = Url.Action("Index", "Pelanggan");
}
```

Then, in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. `<form method="get" asp-action="Create" class="pelanggan-form">`
3. Field **Nama**: label 12px `#5C5C5C`, `<input type="text" name="nama" placeholder="Tulis nama" autocomplete="name">`, rule
4. Field **Nomor HP**: label, `<input type="tel" name="nomorHp" placeholder="Tulis nomor HP" autocomplete="tel">`, rule
5. Field **Alamat**: label, `<textarea name="alamat" placeholder="Tulis alamat" rows="2" autocomplete="street-address">`, rule
6. Actions row, two equal-width 48px / 8px-radius controls:
   - **Batal**: `<a>` to Index, background `#000000`, white Figtree semibold 16px
   - **Simpan pelanggan**: `<button type="submit">`, background `#1877F2`, white Figtree semibold 16px

GET submit re-renders Create with empty fields (query string may appear; ignore it, do not bind or store). That is the only Simpan behavior.

## Styling

Add form-specific classes in `site.css`. Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Fields: no heavy boxes. Label then value, 6px gap, 12px vertical padding, 16px Figtree on the value, 1px `#E5E5E5` rule under each field. Placeholder `#5C5C5C`. Visible `:focus-visible` ring (do not use `outline: none` without a replacement).

**Batal** and **Simpan** sit in a flex row, gap 12px, `flex: 1`, min-height 48px, tap target at least 44px.

Style `a.cta-catat-bon` so the list CTA still fills the width after it becomes an anchor.

Do not restyle Dashboard, Barang, Bon, Privacy, or Error beyond the optional header back.

Keep Bootstrap CSS/JS as already referenced.

## Data and errors

No ViewModels. Controller `Create()` returns `View()` with no arguments.

No empty, loading, or error UI on this page: the slice always shows the empty form. Existing `HomeController.Error` is unchanged.

## Verification

With `dotnet watch run`:

1. `/Pelanggan`: **Tulis nama baru** opens `/Pelanggan/Create`. Header title **Nama baru**. Pelanggan tab active (`#1877F2`). Back chevron visible.
2. Back and **Batal** return to `/Pelanggan`. Other pages have no back chevron.
3. Placeholders read Tulis nama / Tulis nomor HP / Tulis alamat. Typing works. **Simpan pelanggan** does not add a card; it returns to the empty form.
4. Keyboard: Tab through back, fields, Batal, Simpan. Focus ring visible. Desktop: narrow centered shell; mobile: form scrolls above the bottom nav.

## Non-goals

Login, database, validation messages, prefilled query binding, customer detail, edit/delete, Tailwind utilities from the export, pixel-perfect Phosphor icons.
