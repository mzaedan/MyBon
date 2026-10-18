# Barang cari (live search) plan

**Goal:** Make the search row on `/Barang` real, the way `/Pelanggan` already works: type into **Cari barang** and the card list narrows as you type.

**Spec:** `docs/superpowers/specs/2026-09-27-barang-list-form-slicing-design.md` (the "list search is visual only" constraint is superseded by an update note)

**Design read:** the same stock list, now readable by keyboard, for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm; the field wears the export's muted look, focus is the only motion, the one accent stays the CTA).

**Architecture:** The same two pieces the Pelanggan search uses — `SearchAsync` on the service (ILIKE over `nama` only) plus a debounced AJAX call in `wwwroot/custom/features/barang/index.js` that swaps the `_Cards` partial into `#barang-card-stack`. No new model, no migration, no new JS library. The row keeps the export look: it was a `<span>` inside an `aria-hidden` `div`; it becomes the same row with a real `<input type="search">` inside a `GET` form.

## File map

- Modify: `Domain/Interfaces/IBarangService.cs` (`SearchAsync`)
- Modify: `Domain/Services/BarangService.cs` (`SearchAsync`, `ToLikePattern`)
- Modify: `Controllers/BarangController.cs` (`Index(string? q)`, new `Search(string? q)`)
- Modify: `Views/Barang/Index.cshtml` (real search form, count line, `data-search-url`)
- Modify: `Views/Barang/_Cards.cshtml` (no-match notice)
- Modify: `wwwroot/custom/features/barang/index.js` (debounce, AJAX, count line)
- Modify: `wwwroot/css/site.css` (`.barang-search-input`, `.barang-search:focus-within .page-rule`, `.barang-search-empty`, `.barang-hidden`)
- Modify: `docs/superpowers/specs/2026-09-27-barang-list-form-slicing-design.md` (update note)

---

## Slice 1: the query

- [x] **Step 1:** Add `Task<ErrorOr<IReadOnlyList<Barang>>> SearchAsync(string? keyword)` to `IBarangService`, documented as matching item names only.
- [x] **Step 2:** Implement it in `BarangService` the way `PelangganService.SearchAsync` works: normalize the keyword, delegate to `GetAllAsync` when it is blank, otherwise `EF.Functions.ILike(b.Nama, pattern, LikeEscapeCharacter)` with `\`, `%`, and `_` escaped, ordered by `CreatedAt` descending. Only `Nama` is in the predicate, so a satuan typed in the box matches nothing.
- [x] **Step 3:** Message on no match: **Tidak ada barang yang cocok dengan "term"**; otherwise **Ditemukan N barang**.

## Slice 2: the round trip

- [x] **Step 1:** `Index` takes `string? q`, calls `SearchAsync`, and sets `ViewData["SearchQuery"]` so the first paint can be filtered.
- [x] **Step 2:** Add `Search(string? q)`: AJAX callers get `PartialView("_Cards", data)`; a plain page request renders `Index` so `/Barang/Search?q=...` still works without scripting.

## Slice 3: the screen

- [x] **Step 1:** In `Views/Barang/Index.cshtml`, replace the `aria-hidden` `<div>` + `<span>Cari barang</span>` with a `GET` form (`role="search"`) holding `<input type="search" name="q" id="barang-search-input" placeholder="Cari barang" aria-label="Cari nama barang">`, keeping the `bi-search` icon and the rule under the row.
- [x] **Step 2:** The count line becomes **N barang cocok** while a keyword is active and **N barang di buku** when the box is empty; it carries `id="barang-list-title"` and hides when nothing matches. The stack carries `data-search-url="@Url.Action("Search", "Barang")"`.
- [x] **Step 3:** The empty state keeps its own branch: it only paints for an empty notebook with no keyword, so a keyword with no match shows the search row plus the no-match notice instead of the Lottie.
- [x] **Step 4:** Add `<p class="barang-search-empty">Belum ada barang yang cocok. Coba kata kunci lain.</p>` to `_Cards.cshtml` when the list is empty, so the AJAX response carries it too.

## Slice 4: the script

- [x] **Step 1:** In `wwwroot/custom/features/barang/index.js`, debounce 250 ms, `GET <data-search-url>?q=<term>` with `X-Requested-With: XMLHttpRequest`, replace only the stack, abort the in-flight request on a newer keystroke, and replace the stack with **Gagal memuat hasil pencarian. Coba lagi.** on failure.
- [x] **Step 2:** Enter submits through the script when scripting is on and through the form when it is not.
- [x] **Step 3:** The existing Lottie bootstrap keeps its own `if`, and the load-more handler stays delegated on the stack so it survives every search re-render.

## Slice 5: the styles

- [x] **Step 1:** Group `.barang-search-input` (plus `::placeholder` and `:focus`) with `.pelanggan-search-input` — transparent, borderless 15px field filling the row.
- [x] **Step 2:** Add `.barang-search:focus-within .page-rule { background: var(--text); }` beside the Pelanggan focus rule. The form is the anchor because the rule is the row's **sibling**: `.barang-search-row:focus-within .page-rule` is a descendant selector and never matches.
- [x] **Step 3:** Group `.barang-search-empty` with `.pelanggan-search-empty`, and `.barang-hidden` with `.pelanggan-hidden`.
- [x] **Step 4:** Drop the now-unused `.barang-search-text` selector from the group it shared with Pelanggan and Bon; Bon keeps `.bon-search-text`.

---

## Behaviour

| State | What paints |
|---|---|
| Notebook empty, no keyword | Existing `.barang-empty` Lottie state |
| Keyword empty | Every card, count **N barang di buku** |
| Keyword typed | Matching cards, count **N barang cocok** |
| Keyword with no match | No cards, count hidden, **Belum ada barang yang cocok. Coba kata kunci lain.** |
| More than 8 matches | First eight, then **Muat barang berikutnya**; each click reveals eight more |
| Request fails | **Gagal memuat hasil pencarian. Coba lagi.** in place of the cards |

## Deliberately not done

- Searching satuan or harga: the request is "search by item name only", so the predicate stays on `nama`.
- Server-side paging, sorting options, remembering the last keyword, highlighting matches.
- A new JS library or a JSON endpoint: jQuery is already loaded, and the search reuses the cards partial, like Pelanggan.
- Touching `Views/Pelanggan/*` or its focus rule. Noted while building: the existing `.pelanggan-search-row:focus-within .page-rule` never matches for the same sibling reason, so Pelanggan's rule does not darken on focus. It is left as-is here; fixed later in `docs/plans/plan-perbaikan-focus-cari-pelanggan.md`.

## Verification

Checked on the machine with `dotnet run` against the Postgres container:

- [x] `node --check wwwroot/custom/features/barang/index.js` passes.
- [x] `dotnet build` succeeds: 0 warnings, 0 errors.
- [x] `/Barang` paints the search row (`.barang-search-input`, `data-search-url="/Barang/Search"`) and the whole list: **11 barang di buku**.
- [x] `/Barang?q=tella` paints **1 barang cocok** with the card `Q tella`; `/Barang?q=zzz` paints **0** with **Belum ada barang yang cocok. Coba kata kunci lain.**, no cards, and the box keeps the term (`value="zzz"`).
- [x] Name is the only match: `/Barang?q=pcs` — a satuan that exists in the table — paints **0 barang cocok** and no cards.
- [x] `GET /Barang/Search?q=a` with `X-Requested-With: XMLHttpRequest` returns the `_Cards` partial only (no layout), and the log shows `WHERE b.nama ILIKE @pattern ESCAPE '\' ORDER BY b.created_at DESC`.

Needs a browser, still open:

- [ ] Typing narrows the cards as you type (250 ms debounce) and the count line follows; clearing the box restores every card and **N barang di buku**.
- [ ] With JavaScript off, Enter reloads `/Barang?q=...` through the form.
- [ ] **Muat barang berikutnya** still reveals eight more and removes itself on the last batch, and a revealed card still opens `/Barang/Details/{id}`.
- [ ] Keyboard: Tab reaches the input, its rule darkens on focus, Enter does not reload the page.
