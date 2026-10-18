# Pelanggan muat berikutnya backend plan

**Goal:** Make **Muat nama berikutnya** on `/Pelanggan` ask the server for the next eight customers. The first paint carries eight cards, not the whole notebook with the rest hidden.

**Spec:** `docs/superpowers/specs/2026-09-27-pelanggan-list-slicing-design.md` (its "visual only" note for the button, and `docs/superpowers/specs/2026-10-17-cari-pelanggan-live-search-design.md`'s "paging on the server" non-goal, are superseded by update notes)

**Architecture:** The button stays a `<button type="button" class="pelanggan-load-more">` and the UI does not change: one control, no page numbers, no `?page=` deep link, no URL change. What changes is where the rows come from. `IPelangganService.SearchAsync(keyword, page)` now runs `Count` plus `Skip/Take` in the database, sized by a private `PageSize = 8`, and returns `PagedResult<Pelanggan>` (`Items`, `Total`, `Page`, `PageSize`, `HasMore`). `Index` asks for page 1; `Search` takes the page number and answers an XHR with the same `_Cards` partial. The partial renders the button only while `HasMore`, carrying `data-next-page`; the script fetches that page and appends the cards it gets back, then swaps in the button the response ships. So the server still owns both the button markup and the "last page" decision, and the client holds no page state of its own.

Barang is untouched: `Views/Barang/_Cards.cshtml` still hides rows past the eighth, and `.barang-card.barang-card--hidden` stays in `site.css`.

## File map

- Modify: `Domain/Interfaces/IPelangganService.cs` (`SearchAsync(keyword, page)`, `GetAllAsync` removed — no other caller)
- Modify: `Domain/Services/PelangganService.cs` (`PageSize`, `Count` + `Skip/Take`, `Id` tie-breaker)
- Modify: `Controllers/PelangganController.cs` (`Index` = page 1, `Search(q, page = 1)`)
- Modify: `Views/Pelanggan/_Cards.cshtml` (`PagedResult` model, `data-total`, `data-next-page`, no hidden class)
- Modify: `Views/Pelanggan/Index.cshtml` (`Model.Total` instead of `Model.Count`)
- Modify: `wwwroot/custom/features/pelanggan/index.js` (load-more fetches a page, title reads `data-total`)
- Modify: `wwwroot/css/site.css` (drop the now-dead `.pelanggan-card.pelanggan-card--hidden` selector)
- Create: `Models/Common/PagedResult.cs`
- Modify: the two specs above (update notes)

---

## Slice 1: the paged read

- [x] **Step 1:** Add `Models/Common/PagedResult.cs` with `Items`, `Total`, `Page`, `PageSize`, and `HasMore => Page * PageSize < Total`.
- [x] **Step 2:** `PelangganService.SearchAsync(string? keyword, int page)` builds the `ILIKE` query, orders by `CreatedAt` descending then `Id` descending, and returns one page. `ThenByDescending(Id)` keeps the order stable when two rows share a `CreatedAt`, which would otherwise let a page repeat or skip a customer.
- [x] **Step 3:** `PageSize` is a private const (8). The client sends a page number only, so a caller cannot ask the database for the whole notebook.
- [x] **Step 4:** A page below 1 reads as page 1; `GetAllAsync` is deleted because `SearchAsync` already covers the blank keyword.

## Slice 2: the partial and the button

- [x] **Step 1:** `_Cards.cshtml` takes `PagedResult<Pelanggan>`, drops the `pelanggan-card--hidden` class and `data-page-size`, and carries `data-total` so the title can report every match.
- [x] **Step 2:** The button renders while `HasMore` and carries `data-next-page="@(Model.Page + 1)"`; the last page renders no button at all.
- [x] **Step 3:** `Index` paints page 1 and labels the list with `Total`, not with the page.

## Slice 3: the click

- [x] **Step 1:** The delegated handler reads `data-next-page`, aborts an in-flight request, and `GET`s `q` + `page` with `X-Requested-With: XMLHttpRequest`.
- [x] **Step 2:** The response is parsed detached, its cards are appended to the existing `.pelanggan-cards`, and the old button is replaced by the one the response carries — or removed when the response has none. Nothing is revealed: every card appended came from the server.
- [x] **Step 3:** On failure the message **Gagal memuat nama berikutnya. Coba lagi.** sits under the button, which stays clickable so the same page can be asked for again.
- [x] **Step 4:** `updateTitle()` reads `data-total` instead of counting `.pelanggan-card` nodes, so the count line keeps reporting the matches that exist rather than the eight on screen.
- [x] **Step 5:** The search path clears `pending` only when its own request is still the newest, so an aborted request cannot blank the slot its successor filled.

## Slice 4: the styles

- [x] **Step 1:** Remove `.pelanggan-card.pelanggan-card--hidden` from the grouped rule and retitle the comment; `.barang-card.barang-card--hidden` stays for the Barang reveal.

---

## Verification

1. `dotnet build` — clean.
2. Seed more than sixteen customers, then open `/Pelanggan` and view source: eight `<a class="pelanggan-card">` and one button, no hidden card.
3. DevTools Network: one XHR per click — `GET /Pelanggan/Search?q=&page=2`, then `page=3`; the button disappears on the last page.
4. With a keyword matching more than eight names: the count line reads **N nama cocok** for every match, and load-more keeps the keyword.
5. Delete a customer and edit one: the list returns to page 1 unchanged.
6. Barang still behaves exactly as before (reveal, no XHR on click).
