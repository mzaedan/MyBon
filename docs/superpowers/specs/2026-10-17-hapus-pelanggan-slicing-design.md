# CatatHutang Hapus Pelanggan slicing

Date: 2026-10-17  
Source: `d:\coding\MyBon\MyBon\CatatHutang-export.html` (screen `data-pencil-name="Hapus Pelanggan"`)  
App: ASP.NET Core MVC MyBon  
Depends on: shell in `_Layout.cshtml`, Pelanggan list + detail slices, form tokens in `wwwroot/css/site.css`, Lottie asset `wwwroot/lottie/Trash.json`

Design read: confirmation screen before a customer leaves the notebook, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, one illustration, no customer fields).

## Goal

Slice the delete confirmation screen: header back, trash illustration, the consequence copy **Nama, nomor HP, dan alamat hilang dari daftar.**, and the two choices **Batal** / **Hapus data**. The confirm POST keeps deleting for real and returns to the list with `TempData["PelangganStatus"]`.

## Constraints (agreed)

- The design shows no customer fields, only the illustration and the consequence copy. Drop the Nama / Nomor HP / Alamat block from the old slice.
- Button label follows the design: **Hapus data**. The entry point on the detail screen stays **Hapus dari buku**.
- **Batal** and the header back return to the detail sheet of the same customer (`/Pelanggan/Details/{id}`) and delete nothing.
- Illustration is the existing Lottie `wwwroot/lottie/Trash.json`, loaded with the same `lottie-web` CDN build the list already uses. No Phosphor SVG paths from the export.
- No Tailwind CDN, no iPhone status bar, no Phosphor tab bar — the shell bottom nav stays.
- Blue accent `#1877F2` for the confirm button even though the action is destructive: the design does not introduce a red token.
- Out of scope: soft delete, undo, redirect to the detail sheet, per-row copy, Tailwind, Phosphor icons.

## Architecture

Shell stays shared. Delete remains the existing GET/POST pair on `PelangganController`, so the Pelanggan tab stays active. Only the view is re-sliced.

```
Views/Pelanggan/Delete.cshtml              confirm screen markup
wwwroot/custom/features/pelanggan/delete.js  Trash Lottie bootstrap
wwwroot/css/site.css                       .pelanggan-delete* classes
PelangganController                        unchanged (Delete / DeleteConfirmed)
```

URL: `/Pelanggan/Delete/{id}`.  
`ViewData["Title"]` = `Hapus dari buku`.  
`ViewData["BackHref"]` = `Url.Action("Details", "Pelanggan", new { id = Model.Id })`.  
No ViewModels or services added; the view keeps the `Pelanggan` entity as its model for the id only.

## Page content (`Views/Pelanggan/Delete.cshtml`)

In this order:

1. Horizontal rule `#E5E5E5` (`.page-rule`)
2. Delete block, vertically centred in the free space:
   - 220×180 art box holding a 180×180 Lottie container (`id="lottie-trash"`)
   - Copy **Nama, nomor HP, dan alamat hilang dari daftar.** — 15px/21px Figtree `#000000`, centred
3. `<form method="post" asp-action="Delete">` with `asp-for="Id"` hidden input and two equal-width 48px / 8px-radius controls:
   - **Batal**: `<a>` to `Details` of the same customer, background `#000000`, white Figtree semibold 16px
   - **Hapus data**: `<button type="submit">`, background `#1877F2`, white Figtree semibold 16px

Reuse `.pelanggan-form-actions`, `.pelanggan-btn`, `.pelanggan-btn-batal` (Batal) and `.pelanggan-btn-simpan` (Hapus data) so the buttons stay identical to the Create / Details actions row. `.pelanggan-delete-actions` only resets the inherited `margin-top` because the block already spaces its children with `gap: 24px`.

## Scripts

`Index.cshtml` already pulls `lottie-web` 5.12.2 from cdnjs in its `Scripts` section; Delete does the same, then loads `~/custom/features/pelanggan/delete.js`. The script only runs when `#lottie-trash` exists and plays `Trash.json` with `loop: true`, matching `index.js`.

## Styling

Append to `site.css` next to the other pelanggan blocks, reusing tokens (`#FFFFFF`, `#000000`, `#5C5C5C`, `#1877F2`, `#E5E5E5`):

- `.pelanggan-delete` column, `gap: 24px`, `min-height: 100%`, bottom padding — same pattern as `.pelanggan-empty` so the block centres inside the scrolling main area.
- `.pelanggan-delete-body` `flex: 1` with centred column, `gap: 16px`.
- `.pelanggan-delete-art` 220×180 and `.pelanggan-delete-lottie` 180×180, the same dims the empty state uses for the notebook art.
- `.pelanggan-delete-copy` 15px/21px centred, max-width 280px so the copy wraps like the mockup.
- `.pelanggan-delete-actions` `margin-top: 0`.

Do not restyle Dashboard, Barang, Bon, Privacy, or Error beyond inheriting the shell.

## Data and errors

`Delete(int id)` still loads the customer and hands it to the view; an unknown id keeps returning 404 through `ToActionResult`. `DeleteConfirmed` still deletes, sets `TempData["PelangganStatus"]` and redirects to `/Pelanggan`. AJAX callers keep receiving the `ErrorOr` payload.

No empty, loading, or partial-error UI on this screen: the confirmation always renders.

## Verification

With `dotnet watch run`:

1. `/Pelanggan` → open a customer → **Hapus dari buku** opens `/Pelanggan/Delete/{id}`. Header title **Hapus dari buku**, back chevron and Pelanggan tab active.
2. Trash animation plays in the art box. Copy reads **Nama, nomor HP, dan alamat hilang dari daftar.**
3. Back and **Batal** return to `/Pelanggan/Details/{id}`; the customer still exists.
4. **Hapus data** deletes the customer, lands on `/Pelanggan` with the status notice, and the card is gone.
5. Keyboard: back → Batal → Hapus data, focus ring visible. Desktop: narrow centred shell; mobile: content sits above the bottom nav.

## Non-goals

Soft delete, undo, typed confirmation, per-customer copy, redirect targets other than the list, Tailwind utilities from the export, pixel-perfect Phosphor icons.

> Update: the **Hapus data** submit now wears `.pelanggan-btn pelanggan-btn-hapus`, the shared `#B00020` destructive red, instead of `.pelanggan-btn-simpan` accent blue; this supersedes the blue-accent constraint above. **Batal** stays `.pelanggan-btn-batal` black, and the POST, `TempData["PelangganStatus"]`, and redirect behaviour are unchanged. See `docs/plans/plan-tombol-hapus-merah.md`.
