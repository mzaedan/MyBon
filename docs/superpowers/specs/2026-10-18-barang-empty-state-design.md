# CatatHutang Barang empty state design

Date: 2026-10-18  
App: ASP.NET Core MVC MyBon  
Depends on: Barang list slice (`Views/Barang/Index.cshtml`), `IBarangService.GetAllAsync`, Lottie asset `wwwroot/lottie/Notebook with Pencil Icon Animation.json`, empty-state pattern in `Views/Pelanggan/Index.cshtml`

Design read: an empty stock list for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm, one illustration, one action, no controls that cannot act).

Supersedes the "Do not add empty, loading, or error UI on these two pages" line of `2026-09-27-barang-list-form-slicing-design.md` for the list only.

## Goal

Give `/Barang` the same empty state the Pelanggan list already has, so a shopkeeper who has not recorded any item yet sees an illustration, a sentence that says what to do, and one button that opens the write-item form.

## Constraints (agreed)

- Mirror Pelanggan: the empty state replaces the list body, it is not a notice inside a populated page.
- Reuse the existing notebook Lottie and the existing `lottie-web` CDN build (5.12.2), the same pair the Pelanggan list and the Pelanggan delete screen load. No new Lottie library.
- No new Lottie asset: `wwwroot/lottie/` holds only the notebook and the trash animation. The copy already says **di buku**, so the notebook illustration matches the sentence.
- The button is the existing `a.cta-catat-bon` (48px, `#1877F2`, 8px radius, white Figtree semibold 16px) pointing at `Barang/Create`. It is the same control the populated list shows, so it is not cloned into a second class.
- Out of scope: live search on `/Barang`, item detail, edit/delete, loading state, server-side paging.

## Architecture

```
Views/Barang/Index.cshtml                    empty branch + Scripts section
wwwroot/custom/features/barang/index.js      Lottie bootstrap
wwwroot/css/site.css                         .barang-empty* block
BarangController.Index()                     unchanged (GetAllAsync -> View)
```

ID `lottie-barang-empty` follows the `lottie-*` ids already used by `lottie-notebook` and `lottie-trash`.

## Behaviour

1. `Model.Count == 0` renders the empty state: rule, illustration, copy, **Tulis barang baru**.
2. The search row and the **N barang di buku** count line are not rendered while the list is empty: the row is `aria-hidden` decoration that could not filter anything, and "0 barang di buku" only repeats the copy. Pelanggan already hides both when its list is empty.
3. `Model.Count > 0` renders the list slice exactly as before: search row, count, cards, **Tulis barang baru**.
4. The animation autoplays and loops, like the other two Lottie screens. With scripting off the reserved 220x200 art box stays empty; copy and button still work.

## Styling

Appended in `site.css` as its own `/* ── Barang empty state ── */` block, reusing tokens and copying the `.pelanggan-empty*` metrics (the file keeps screen-level components self-contained, the way `.pelanggan-delete*` mirrors `.pelanggan-empty*` instead of grouping with it):

- `.barang-empty`: column, `gap: 24px`, `min-height: 100%`, bottom padding, so the block sits centred in `main`.
- `.barang-empty-body`: `flex: 1`, column, `gap: 16px`, centred both axes.
- `.barang-empty-art`: 220x200 reserved box; `.barang-empty-lottie`: 200x200.
- `.barang-empty-copy`: 15px/21px `--text`, centred, `max-width: 280px`.

Reuse tokens: text `#000000`, muted `#5C5C5C`, accent `#1877F2`, border/rule `#E5E5E5`, surface `#FFFFFF`.

## Data and errors

`Index()` and the read path are untouched: no matches and an empty notebook are the same successful empty list. `HomeController.Error` still owns failures. The empty state is a state of a successful read, not an error screen.

## Reasons (R-31)

- **Why an illustration here**: the screen has one job (write the first item); the notebook drawing is the product's own motif and already means "the book is empty, write in it".
- **Why the button is the shared CTA**: it opens the same `Barang/Create` as the populated list, so one class keeps one height, colour and focus ring.
- **Why the search row disappears**: a control that cannot filter is a dead control; on an empty list there is nothing to search.
- **Why centered and full height**: it is the only block on the screen, so it takes the focal point instead of hugging the top.

## Verification

With `dotnet watch run` and the Postgres container up:

1. `/Barang` with an empty `Barangs` table: rule, notebook animation, **Belum ada barang di buku. Tulis barang baru untuk mulai mencatat.**, and the blue **Tulis barang baru** button. No search row, no count line.
2. Clicking the button opens `/Barang/Create` with an empty form; back returns to `/Barang`.
3. Inserting one item and reloading `/Barang` shows the search row, **1 barang di buku**, the card, and no illustration.
4. Keyboard: Tab reaches the button, focus ring visible. Mobile: the block stays centred above the bottom nav; desktop: no overflow at 390px shell width.

## Non-goals

Live search on the item list, item detail/edit/delete, a dedicated item illustration, loading skeleton, Tailwind utilities, Phosphor icons.

> Update: item edit now exists for the populated list (`/Barang/Edit/{id}`). Live search, item detail, and delete are still out of scope. See `2026-10-18-barang-edit-design.md`.

> Update: item detail now exists too (`/Barang/Details/{id}`), reached from the card. Live search and delete are still out of scope. See `2026-10-18-barang-detail-design.md`.
