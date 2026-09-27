# CatatHutang Catat Bon list slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html` (screen `data-pencil-name="Catat Bon"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml` (header MY BON + title, four-tab bottom nav)

Design read: mobile bon list for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, uniform cards, focus only on real controls in the shell).

## Goal

Replace the Bon placeholder with a static list slice: search row, count, four bon cards, visual **Catat bon baru**. No database. No create form.

## Constraints (agreed)

- Approach: mirror Barang Index (markup in `Views/Bon/Index.cshtml` + CSS in `site.css`). No shared partials. No `Bon/Create`.
- Search is visual only: icon + text **Cari nomor atau nama** + rule. Not an `<input>`, not a `<form>`, no filtering.
- Cards are `div`s: no `href`, no detail page.
- **Catat bon baru** is a `div`: not an `<a>`, not a `<button>`, no `href`, no click handler. `aria-hidden="true"`.
- Do not add Tailwind CDN. Do not copy Phosphor SVG paths. Search uses Bootstrap Icons `bi-search`.
- Do not render the mockup iPhone status bar.
- Do not duplicate the header inside the page. Layout wordmark stays **MY BON**.
- Out of scope: login, database, live search, bon detail, edit/delete, form catat bon, Tailwind, Phosphor paths from the export.

## Architecture

Shell stays shared. Only Index changes.

```
_Layout.cshtml           unchanged
BonController
  Index()                list; no Create action
Views/Bon/Index.cshtml   list markup
wwwroot/css/site.css     bon list classes; search may share Pelanggan/Barang selectors
```

URL: `/Bon`.  
`ViewData["Title"]` = `Catat Bon`.  
Do not set `ViewData["BackHref"]`.  
No ViewModels, services, or database. Do not change Pelanggan, Barang, Dashboard, Privacy, or Error views.

## List (`Views/Bon/Index.cshtml`)

```html
@{
    ViewData["Title"] = "Catat Bon";
}
```

Static markup in this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. Search row: `bi-search` (18px, `#5C5C5C`) + **Cari nomor atau nama** (Figtree 15px, `#5C5C5C`) + rule. Mark the block `aria-hidden="true"` like Pelanggan and Barang.
3. List title **4 bon di buku** (Figtree 13px semibold, `#5C5C5C`). Count is hardcoded.
4. Four cards, gap 8px. Each card is a `div`: white, 1px outline `#E5E5E5`, radius 10px, padding 12px, inner column gap 6px:
   - Top row, space-between: nomor (Figtree 13px, `#5C5C5C`) and status (Figtree 13px semibold)
   - Nama pelanggan (Figtree 15px semibold, `#000000`)
   - Tanggal (Figtree 13px, `#5C5C5C`)
   - Amounts row, space-between, gap 16px: **Total belanja** left, **Sisa hutang** right. Labels Figtree 12px `#5C5C5C`. Values Fraunces 16px semibold `#000000`, nowrap. Sisa column is right-aligned.
5. Full-width **Catat bon baru** as `<div class="bon-cta" aria-hidden="true">`. Same size and color as `a.cta-catat-bon` (48px height, `#1877F2`, 8px radius, white Figtree semibold 16px). `cursor: default`. No `:active` dim. Not in the tab order.

Status color: **Belum lunas** `#000000`. **Lunas** `#5C5C5C` (BN-011 only). Use a class such as `.bon-card-status--lunas` for the muted status.

The list scrolls in `main`. The CTA sits after the cards in document order, not sticky above the nav. Do not add **Muat berikutnya**.

Do not reuse `.cta-catat-bon` on this element: that class sets `cursor: pointer` and is used on real links and buttons elsewhere.

### Hardcoded rows (from the export mockup, not live shop data)

| No | Nama | Tanggal | Total belanja | Sisa hutang | Status |
|---|---|---|---|---|---|
| BN-014 | Siti Aminah | 12 Maret 2026 | Rp 185.000 | Rp 185.000 | Belum lunas |
| BN-013 | Budi Santoso | 10 Maret 2026 | Rp 92.000 | Rp 92.000 | Belum lunas |
| BN-012 | Rina Wahyuni | 8 Maret 2026 | Rp 48.500 | Rp 48.500 | Belum lunas |
| BN-011 | Agus Salim | 5 Maret 2026 | Rp 40.000 | Rp 0 | Lunas |

No avatars, no extra columns.

## Styling

Add Bon-list classes in `site.css` (for example `.bon-page`, `.bon-search`, `.bon-card`, `.bon-card-no`, `.bon-card-status`, `.bon-card-name`, `.bon-card-date`, `.bon-amounts`, `.bon-amount-col`, `.bon-amount-label`, `.bon-amount-value`, `.bon-cta`). Group search and list-title selectors with Pelanggan/Barang when metrics match. Do not restyle Pelanggan or Barang cards to look like Bon cards.

Reuse tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Reuse `.page-rule`. Amounts use Fraunces because the export does; labels, names, and status stay Figtree.

Keep Bootstrap CSS/JS as already referenced.

## Data and errors

No ViewModels. `Index()` returns `View()` with no arguments.

The list always shows the four mockup rows. Do not add empty, loading, or error UI on this page. Existing `HomeController.Error` is unchanged.

## Verification

With `dotnet watch run`:

1. `/Bon`: title **Catat Bon**, Catat Bon tab active (`#1877F2`), search text **Cari nomor atau nama**, line **4 bon di buku**, four cards with nomor / status / nama / tanggal / total / sisa, visual **Catat bon baru**.
2. Clicking a card or the CTA does not change the URL. `/Bon` has no back chevron.
3. BN-011 status **Lunas** is `#5C5C5C`. The other three statuses **Belum lunas** are `#000000`. BN-011 sisa is `Rp 0`.
4. Cards are not links. Search is not an input. CTA is not a link or button.
5. Keyboard: Tab does not land on cards, search, or CTA. Shell tabs remain reachable. Desktop: narrow centered shell; mobile: list scrolls above the bottom nav.

## Non-goals

Login, database, live search, bon detail, edit/delete, Catat bon form, `Bon/Create`, Tailwind utilities from the export, pixel-perfect Phosphor icons.
