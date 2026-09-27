# CatatHutang shell slicing

Date: 2026-09-27  
Source: `d:\coding\Design\CatatHutang-export.html`  
App: ASP.NET Core MVC MyBon

## Goal

Replace the current generic mobile shell with a visual slice of the CatatHutang dashboard: shop header, dashboard content, and a four-tab bottom navigation bar. This is layout and static UI only.

## Constraints (agreed)

- Restyle the existing MVC shell (`Views/Shared/_Layout.cshtml` + `wwwroot/css/site.css`). Do not add Tailwind CDN.
- Split navigation into four controllers named after the tabs. Dashboard is `DashboardController`, not `HomeController`.
- Dashboard numbers and names are hardcoded in the view. No models, services, or database.
- Primary CTA **Catat bon baru** is visual only: `button` or non-navigating control, not an `asp-action` link.
- Do not render the mockup iPhone status bar (time, signal, wifi, battery).
- Privacy and Error pages stay; they are not bottom-nav items.
- Out of scope: login, CRUD, catat-bon form, pelanggan/barang lists, Tailwind.

## Architecture

Shared layout is the app chrome. Pages only fill `main`.

```
_Layout.cshtml
  header.app-header     brand mark + shop name + ViewData["Title"]
  main.app-main         @RenderBody()
  nav.bottom-nav        4 tabs, active from current controller
```

| Tab | Controller / action | Header title |
|---|---|---|
| Dashboard | `DashboardController.Index` | Dashboard |
| Pelanggan | `PelangganController.Index` | Pelanggan |
| Barang | `BarangController.Index` | Barang |
| Catat Bon | `BonController.Index` | Catat Bon |

Default route: `{controller=Dashboard}/{action=Index}/{id?}`.

`HomeController` remains only for `Privacy` and `Error`. Those URLs highlight no bottom-nav tab.

Active tab: compare current controller name to the tab’s controller (`Dashboard`, `Pelanggan`, `Barang`, `Bon`).

## Header

Always in `_Layout.cshtml`:

- 28px black rounded square with white **C** (Fraunces semibold).
- Shop line: **Toko Kelontong Berkah** (Figtree, 12px, `#5C5C5C`). Hardcoded for this slice.
- Screen title: `@ViewData["Title"]` (Figtree, 22px, black). Each view sets Title.

No notification bell. No extra header actions.

## Bottom navigation

Four equal tabs in a pill: white background, 1px `#E5E5E5` outline, 16px corner radius, horizontal inset ~16px.

Items: Dashboard (house), Pelanggan (users), Barang (package), Catat Bon (notepad).

- Inactive: icon + label `#5C5C5C`.
- Active: `#1877F2`.
- Labels 10px Figtree.

Use these Bootstrap Icons (do not copy Phosphor SVG paths from the export): `bi-house` / `bi-house-fill`, `bi-people` / `bi-people-fill`, `bi-box-seam` / `bi-box-seam-fill`, `bi-journal-text`. Filled variant on the active tab only.

Nav items are real `asp-controller` / `asp-action="Index"` links so tab switching works.

On small phones the nav stays at the bottom of the app shell with `safe-area-inset-bottom`. On desktop the shell stays centered with `max-width: 390px` to match the export frame.

## Dashboard content (`Views/Dashboard/Index.cshtml`)

Static markup matching the export, in this order:

1. Horizontal rule `#E5E5E5`
2. Block **Total hutang semua customer** — value `Rp 325.500` (Fraunces 42px, `#1877F2`) — hint **Semua bon yang belum lunas**
3. Horizontal rule
4. Row **Jumlah produk terjual** / **47**
5. Section title **Belum lunas**
6. Three rows: Siti Aminah / Rp 185.000; Budi Santoso / Rp 92.000; Rina Wahyuni / Rp 48.500; each followed by a rule
7. Full-width **Catat bon baru** as `<button type="button">`: 48px height, `#1877F2`, 8px radius, white Figtree semibold 16px. No `href`, no `asp-action`.

Do not duplicate the header inside the page; the layout already owns it.

## Placeholder pages

`Views/Pelanggan/Index.cshtml`, `Views/Barang/Index.cshtml`, `Views/Bon/Index.cshtml`:

- Set `ViewData["Title"]` to Pelanggan, Barang, or Catat Bon.
- One short placeholder sentence (no lists, no forms).

New controllers: `DashboardController`, `PelangganController`, `BarangController`, `BonController`, each with `Index()` returning `View()`. Remove `Index` from `HomeController` (keep `Privacy` and `Error`). Move dashboard markup from `Views/Home/Index.cshtml` to `Views/Dashboard/Index.cshtml`.

## Styling

Update CSS variables in `site.css` to the export tokens:

- Background and surface: `#FFFFFF`
- Text: `#000000`
- Muted: `#5C5C5C`
- Accent: `#1877F2`
- Border/rule: `#E5E5E5`

Load Google Fonts Figtree and Fraunces in `_Layout.cshtml`. Apply Figtree on the shell; Fraunces on the brand **C** and numeric amounts.

Replace the current full-bleed bottom bar (Beranda / Cari / Riwayat / Akun) and the MyBon + bell header.

Leave unused generic helpers in `site.css` in place unless they override the new header, main, or nav. Do not restyle Privacy/Error beyond inheriting the new shell.

Keep Bootstrap CSS/JS as already referenced. Dashboard visuals use new layout-specific classes, not Bootstrap cards.

## Data and errors

No ViewModels for dashboard data in this slice. Hardcoded strings in the Razor view.

Error handling unchanged: existing `HomeController.Error` action and view. Placeholder pages have no failure paths.

## Verification

With `dotnet watch run`:

1. Dashboard shows shop header, static hutang block, list, and CTA; no iOS status bar.
2. Each of the four tabs navigates and sets the matching header title and active color.
3. CTA does not change the URL.
4. Desktop: narrow centered shell; mobile viewport: header + scrollable main + bottom pill nav.

## Non-goals

Login, persistence, linking CTA to Bon, product/customer CRUD, copying Tailwind utility classes from the HTML export, pixel-perfect Phosphor SVG icons.
