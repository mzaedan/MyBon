# CatatHutang Detail Pelanggan slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Detail Pelanggan"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml`, Pelanggan list in `Views/Pelanggan/Index.cshtml`, form tokens in `wwwroot/css/site.css`

Design read: mobile read-only customer sheet for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform fields, focus only).

## Goal

Add a static visual slice of the customer detail screen: header back to the list, read-only Nama / Nomor HP / Alamat for the mockup person **Siti Aminah**, **Ubah data** and **Hapus dari buku**. No database. Every list card opens this same page.

## Constraints (agreed)

- Visual only: Ubah and Hapus do not edit or delete. They go to `Pelanggan/Index`. Siti's card stays on the list.
- All ten list cards are links to the same Details URL. Clicking Budi (or any other name) still shows Siti Aminah.
- Optional back in `_Layout` when `ViewData["BackHref"]` is set. On this page it is `/Pelanggan`.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths; back icon stays Bootstrap Icons `bi-chevron-left`.
- Fields are read-only text, not `<input>`. Values come from the mockup, not live shop data. No avatars.
- Do not render the mockup iPhone status bar.
- Do not change header type hierarchy on other pages.
- Out of scope: database, per-card data, server validation, WhatsApp, real edit/delete, Tailwind, Phosphor from the export.

## Architecture

Shell stays shared. Details is a third action on the same controller so the Pelanggan tab stays active.

```
_Layout.cshtml                  optional back via ViewData["BackHref"]
PelangganController
  Index()                       list; each card links to Details
  Create()                      form (unchanged)
  Details()                     read-only view (GET)
Views/Pelanggan/Index.cshtml    cards become anchors
Views/Pelanggan/Details.cshtml  field markup
wwwroot/css/site.css            card-as-link and read-only field classes
```

URL: `/Pelanggan/Details`.  
`ViewData["Title"]` = `Siti Aminah`.  
`ViewData["BackHref"]` = `/Pelanggan` (or `Url.Action` to Index).  
No ViewModels, services, or database. No query string. Controller ignores which card was clicked.

## Header back (`_Layout.cshtml`)

Reuse the existing optional back. Details sets `BackHref` like Create. Other pages that omit it stay unchanged.

## List change (`Views/Pelanggan/Index.cshtml`)

Replace each `<div class="pelanggan-card">` with:

```html
<a class="pelanggan-card" asp-controller="Pelanggan" asp-action="Details">
    ...
</a>
```

Keep the same inner markup (name, phone, address). **Muat nama berikutnya** stays a visual-only button. **Tulis nama baru** still goes to Create.

## Page content (`Views/Pelanggan/Details.cshtml`)

```html
@{
    ViewData["Title"] = "Siti Aminah";
    ViewData["BackHref"] = Url.Action("Index", "Pelanggan");
}
```

Then, in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. Field **Nama**: label 12px `#5C5C5C`, value **Siti Aminah**, rule
3. Field **Nomor HP**: label, value **0812 3456 7890**, rule
4. Field **Alamat**: label, value **Jl. Melati No. 12, Sukajadi**, rule
5. Actions row, two equal-width 48px / 8px-radius links:
   - **Ubah data**: `<a>` to Index, background `#000000`, white Figtree semibold 16px
   - **Hapus dari buku**: `<a>` to Index, background `#1877F2`, white Figtree semibold 16px

Reuse `.pelanggan-field` and `.pelanggan-field-label`. Value is a `<p>` or `<div class="pelanggan-field-value">`, not an input. Reuse `.pelanggan-form-actions`, `.pelanggan-btn`, `.pelanggan-btn-batal` (Ubah), `.pelanggan-btn-simpan` (Hapus). Add `a.pelanggan-btn-simpan { color: #ffffff; }` if the global `a` color would turn Hapus blue-on-blue.

## Styling

Add only what the list and detail need. Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

**Card as link:** `a.pelanggan-card` keeps the card layout. Color of name and meta stays as today (black / muted), not the global link blue. No underline. Visible `:focus-visible` ring. Cursor pointer. Tap target is the full card.

**Read-only value:** Figtree 16px `#000000`, same as form inputs. No heavy boxes. Label then value, 6px gap, 12px vertical padding, 1px `#E5E5E5` rule under each field.

**Ubah** and **Hapus** sit in a flex row, gap 12px, `flex: 1`, min-height 48px, tap target at least 44px.

Do not restyle Dashboard, Barang, Bon, Privacy, or Error beyond inheriting the shell.

Keep Bootstrap CSS/JS as already referenced.

## Data and errors

No ViewModels. Controller `Details()` returns `View()` with no arguments.

No empty, loading, or error UI on this page: the slice always shows Siti Aminah. Existing `HomeController.Error` is unchanged.

Mockup names on the list and on this page are design fixtures, not live customers.

## Reasons (one line)

- Header title is the person name because the mockup uses the customer as the screen title, not a generic "Detail".
- All cards share one URL so the slice can be clicked through without inventing per-row routing or a database.
- Ubah and Hapus go to the list so every control has a real destination without shipping edit/delete.
- Read-only text instead of inputs because this screen is a sheet to look at, not a form.
- Card links keep existing card chrome so the list does not look like a blue URL list.

## Verification

With `dotnet watch run`:

1. `/Pelanggan`: each of the ten cards opens `/Pelanggan/Details`. Header title **Siti Aminah**. Pelanggan tab active (`#1877F2`). Back chevron visible.
2. Card that reads Budi Santoso still shows Siti Aminah / 0812 3456 7890 / Jl. Melati No. 12, Sukajadi.
3. Back, **Ubah data**, and **Hapus dari buku** return to `/Pelanggan`. Ten cards still present, including Siti.
4. **Tulis nama baru** still opens Create. **Muat nama berikutnya** does not navigate.
5. Keyboard: Tab through cards, then on Details through back, Ubah, Hapus. Focus ring visible. Desktop: narrow centered shell; mobile: content scrolls above the bottom nav.

## Non-goals

Login, database, different detail per card, query-string binding, edit form, confirm-delete, Tailwind utilities from the export, pixel-perfect Phosphor icons.
