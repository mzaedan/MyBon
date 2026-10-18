# Barang muat berikutnya backend plan

**Goal:** Make **Muat barang berikutnya** on `/Barang` ask the server for the next eight items, the way `/Pelanggan` now does. The first paint carries eight cards, not the whole stock list with the rest hidden.

**Spec:** `docs/superpowers/specs/2026-09-27-barang-list-form-slicing-design.md` (its load-more update note is superseded by another update note)

**Architecture:** The same shape as `docs/plans/plan-pelanggan-muat-berikutnya-backend.md`. `IBarangService.SearchAsync(keyword, page)` runs `Count` plus `Skip/Take` in the database, sized by a private `PageSize = 8`, and returns the shared `PagedResult<Barang>`. `Index` asks for page 1; `Search(q, page = 1)` answers an XHR with the same `_Cards` partial, which paints one page and renders **Muat barang berikutnya** only while `HasMore`, carrying `data-next-page`. The click handler fetches that page, appends the cards, and swaps in the button the response ships — or drops the button when the response carries none, which is how the server marks the last page.

The UI does not change: one `<button type="button">`, no page numbers, no `?page=` deep link, no URL change. `PageSize` lives in the service, so the client only ever sends a page number.

## File map

- Modify: `Domain/Interfaces/IBarangService.cs` (`SearchAsync(keyword, page)`, `GetAllAsync` removed — no other caller)
- Modify: `Domain/Services/BarangService.cs` (`PageSize`, `Count` + `Skip/Take`, `Id` tie-breaker)
- Modify: `Controllers/BarangController.cs` (`Index` = page 1, `Search(q, page = 1)`)
- Modify: `Views/Barang/_Cards.cshtml` (`PagedResult` model, `data-total`, `data-next-page`, no hidden class)
- Modify: `Views/Barang/Index.cshtml` (`Model.Total` instead of `Model.Count`)
- Modify: `wwwroot/custom/features/barang/index.js` (load-more fetches a page, title reads `data-total`)
- Modify: `wwwroot/css/site.css` (drop the now-dead `.barang-card.barang-card--hidden` rule — the last user of the `--hidden` pattern)
- Modify: the spec and plan named above (update notes)

---

## Slice 1: the paged read

- [x] **Step 1:** `BarangService.SearchAsync(string? keyword, int page)` builds the `ILIKE` query, orders by `CreatedAt` descending then `Id` descending, and returns one `PagedResult<Barang>` page.
- [x] **Step 2:** `ThenByDescending(Id)` keeps the order stable when two rows share a `CreatedAt`, which would otherwise let a page repeat or skip an item.
- [x] **Step 3:** `PageSize` is a private const (8); a page below 1 reads as page 1; `GetAllAsync` is deleted because `SearchAsync` already covers the blank keyword.
- [x] **Step 4:** Harga formatting stays in the partial, so `Rp 78.000` reads the same on every page.

## Slice 2: the partial, the click, the styles

- [x] **Step 1:** `_Cards.cshtml` takes `PagedResult<Barang>`, drops `barang-card--hidden` and `data-page-size`, and carries `data-total` for the title.
- [x] **Step 2:** The button renders while `HasMore` with `data-next-page="@(Model.Page + 1)"`; the last page renders no button.
- [x] **Step 3:** The delegated handler aborts an in-flight request, `GET`s `q` + `page`, appends the incoming cards, and replaces its own button with the one the response carries. A failure shows **Gagal memuat barang berikutnya. Coba lagi.** and leaves the button clickable.
- [x] **Step 4:** `updateTitle()` reads `data-total`, so the count line reports every match rather than the eight on screen.
- [x] **Step 5:** Remove `.barang-card.barang-card--hidden` from `site.css`; the `--hidden` pattern has no user left.
- [x] **Step 6:** `Index` paints page 1 and labels the list with `Total`.

---

## Verification

1. `dotnet build` — clean.
2. Seed more than sixteen items, then open `/Barang` and view source: eight `<a class="barang-card">` and one button, no hidden card.
3. DevTools Network: one XHR per click — `GET /Barang/Search?q=&page=2`, then `page=3`; the button disappears on the last page.
4. With a keyword matching more than eight names: the count line reads **N barang cocok** for every match; prices still read `Rp 78.000`.
5. Pelanggan behaves the same way on its own screen; Details, Edit, Delete, and Create are unaffected on both.
