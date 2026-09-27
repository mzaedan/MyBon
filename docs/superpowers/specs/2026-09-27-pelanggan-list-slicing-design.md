# CatatHutang Pelanggan list slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Pelanggan"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell already in `_Layout.cshtml` (header MY BON + title, four-tab bottom nav)

## Goal

Replace the Pelanggan placeholder with a static visual slice of the export list: search row, count line, ten customer cards, and a **Tulis nama baru** button. Layout and static UI only.

## Constraints (agreed)

- Fill `Views/Pelanggan/Index.cshtml` + page CSS in `wwwroot/css/site.css`. Do not change `_Layout.cshtml` or bottom nav.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths; use Bootstrap Icons `bi-search` for the search row.
- Hardcoded markup in the Razor view. No models, services, database, or JavaScript filter.
- Search is visual only: icon + text **Cari nama** + rule. Not an `<input>`, not a `<form>`.
- Cards are `div`s: no `href`, no detail page.
- **Tulis nama baru** is `<button type="button">`: no `href`, no `asp-action`.
- Names, phones, and addresses come from the mockup. They are not live shop data. No avatars.
- Do not render the mockup iPhone status bar.
- Out of scope: login, persistence, live search, write-name form, customer detail, changing header or nav.

## Architecture

Shell stays as-is. Only `main` changes.

```
_Layout.cshtml          unchanged chrome
Views/Pelanggan/Index   search row, count, 10 cards, CTA
PelangganController     Index() => View()  (no change)
```

`ViewData["Title"]` remains `Pelanggan`. Tab active state already comes from the controller name.

## Page content (`Views/Pelanggan/Index.cshtml`)

Static markup matching the export, in this order:

1. Horizontal rule `#E5E5E5`
2. Search row: `bi-search` (18px, `#5C5C5C`) + **Cari nama** (Figtree 15px, `#5C5C5C`) + rule
3. List title **10 nama di buku** (Figtree 13px semibold, `#5C5C5C`). Count is hardcoded.
4. Ten cards, gap 8px. Each card: white, 1px outline `#E5E5E5`, radius 10px, padding 14px, inner gap 4px.
   - Name: Figtree 15px semibold, `#000000`
   - Phone: Figtree 13px, `#5C5C5C`
   - Address: Figtree 13px, `#5C5C5C`
5. After the ten cards, still inside the card stack: **Muat nama berikutnya** as `<button type="button" class="pelanggan-load-more">`. Visual only: 44px height, 8px radius, 1px black outline, Figtree semibold 15px, no `href`, no extra rows. Shown because the mockup assumes more than ten names; this slice does not load more.
6. Full-width **Tulis nama baru** as `<button type="button" class="cta-catat-bon">`: same class and metrics as dashboard **Catat bon baru** (48px height, `#1877F2`, 8px radius, white Figtree semibold 16px). Only the label differs.

Do not duplicate the header inside the page. The list scrolls in `main`. The button sits after the cards in document order, not sticky above the nav.

### Hardcoded rows

| Name | Phone | Address |
|---|---|---|
| Siti Aminah | 0812 3456 7890 | Jl. Melati No. 12, Sukajadi |
| Budi Santoso | 0813 2211 4455 | Jl. Kenanga No. 7, Cibeunying |
| Rina Wahyuni | 0857 9900 1122 | Gang Mawar No. 3, Andir |
| Agus Salim | 0819 7788 3344 | Jl. Cihampelas No. 45 |
| Dewi Lestari | 0821 5566 7788 | Jl. Dago No. 8 |
| Hasan Basri | 0852 1100 2233 | Jl. Astanaanyar No. 21 |
| Nur Aisyah | 0815 6677 8899 | Jl. Soekarno Hatta No. 90 |
| Joko Pranoto | 0838 4455 6677 | Jl. Buah Batu No. 16 |
| Lina Marlina | 0878 2233 4455 | Jl. Setiabudi No. 5 |
| Eko Wijaya | 0811 9988 7766 | Jl. Pasteur No. 33 |

## Styling

Add Pelanggan-specific classes in `site.css` (for example `.pelanggan-page`, `.pelanggan-search`, `.pelanggan-card`). Reuse existing tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Reuse `.page-rule` for the horizontal rules. Do not use the unused generic helpers (`.search-input`, `.content-card`). Do not restyle Dashboard, Barang, Bon, Privacy, or Error beyond inheriting the shell.

Keep Bootstrap CSS/JS as already referenced.

## Data and errors

No ViewModels. Hardcoded strings in the view.

No empty, loading, or error UI on this page: the slice always shows ten cards. Existing `HomeController.Error` is unchanged.

## Verification

With `dotnet watch run`:

1. `/Pelanggan`: header title Pelanggan, Pelanggan tab active (`#1877F2`), search row, **10 nama di buku**, ten cards, **Tulis nama baru**.
2. Search does not filter. **Muat nama berikutnya** and **Tulis nama baru** do not change the URL. Cards are not links.
3. Dashboard, Barang, and Catat Bon still work. Desktop: narrow centered shell; mobile: scrollable list above the bottom nav.

## Non-goals

Login, database, filtering, write-name form, customer detail, Tailwind utilities from the export, pixel-perfect Phosphor icons, floating CTA over the nav.
