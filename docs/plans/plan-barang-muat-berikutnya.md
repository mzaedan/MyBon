# Barang muat berikutnya plan

**Goal:** Give `/Barang` the same load-more control the Pelanggan list has, so a long stock list paints eight cards and the button **Muat barang berikutnya** reveals the rest eight at a time.

**Spec:** `docs/superpowers/specs/2026-09-27-barang-list-form-slicing-design.md` (its "Do not add Muat berikutnya" constraint is superseded by an update note)

**Architecture:** The same two pieces the Pelanggan list uses — a `_Cards` partial that hides every card past the eighth and renders the button, plus a click handler in `wwwroot/custom/features/barang/index.js` delegated on the card stack. Client-side only: no controller, service, interface, model, or migration change, and no server-side paging, because `GetAllAsync` already returns the whole stock list.
> Update: the reveal is gone, so the architecture above is superseded. The list now pages on the server, like `/Pelanggan`: `IBarangService.SearchAsync(keyword, page)` returns `PagedResult<Barang>` and the click handler fetches the next page instead of unhiding rows the first paint already carried. See `docs/plans/plan-barang-muat-berikutnya-backend.md`.



## File map

- Create: `Views/Barang/_Cards.cshtml` (batch size, hidden cards, load-more button)
- Modify: `Views/Barang/Index.cshtml` (render the partial inside `.barang-card-stack`)
- Modify: `wwwroot/custom/features/barang/index.js` (reveal on click, delegated on the stack)
- Modify: `wwwroot/css/site.css` (`.barang-card-stack`, `.barang-card.barang-card--hidden`, `.barang-load-more`, grouped with their Pelanggan twins)
- Modify: `docs/superpowers/specs/2026-09-27-barang-list-form-slicing-design.md` (update note)

---

## Slice 1: the partial and the button

- [ ] **Step 1:** Add `Views/Barang/_Cards.cshtml` modelled on `Views/Pelanggan/_Cards.cshtml`: `const int PageSize = 8;`, a `index` counter, the `FormatRupiah` local function the list already used, then the `#barang-cards` container carrying `data-page-size="@PageSize"`.
- [ ] **Step 2:** Append `barang-card--hidden` to every card from `index >= PageSize` on, and render `<button type="button" class="barang-load-more" id="barang-load-more">Muat barang berikutnya</button>` only when `Model.Count > PageSize`.
- [ ] **Step 3:** In `Views/Barang/Index.cshtml`, replace the inline card loop with `<div class="barang-card-stack" id="barang-card-stack"><partial name="_Cards" model="Model" /></div>`, and drop the now-unused `System.Globalization` using, `PageSize`, `index`, and `FormatRupiah`.
- [ ] **Step 4:** In `wwwroot/custom/features/barang/index.js`, keep the Lottie bootstrap behind its own `if` and add a click listener on `#barang-card-stack` that resolves `.barang-load-more` through `event.target.closest()`. Delegating matches the Pelanggan script and survives any re-render of the cards.

## Slice 2: the styles, and the cascade

- [ ] **Step 1:** Add `.barang-card-stack` to the `.pelanggan-card-stack` rule (column, `gap: 8px`) so the button sits one 8px step under the last card, the way it does on Pelanggan.
- [ ] **Step 2:** Add `.barang-load-more` (with `:active` and `:focus-visible`) to the `.pelanggan-load-more` rules: 100% wide, 44px, 8px radius, black outline, Figtree semibold 15px.
- [ ] **Step 3:** Write the hidden rule as `.pelanggan-card.pelanggan-card--hidden, .barang-card.barang-card--hidden { display: none; }`. The doubled class is required: `.barang-card { display: flex; }` is declared ~330 lines *below* the hidden rule with the same specificity, so a one-class selector loses the cascade and every card stays visible. Pelanggan only worked by luck, because `.pelanggan-card` sits above the hidden rule.

## Styling note

Order and specificity both decide this rule; grouping the selector with `.barang-card`'s neighbours is not enough. Keep the doubled class if the rule is ever moved, or move it below both card blocks.

---

## Behaviour

| List size | What paints |
|---|---|
| 0 | Existing `.barang-empty` state, no cards, no button |
| 1–8 | Every card, no button |
| 9+ | First eight cards, then **Muat barang berikutnya**; each click reveals the next eight |
| Last batch revealed | Button removes itself |

## Deliberately not done

- Server-side paging or a `Skip`/`Take` query: the controller already ships the whole notebook, and the Pelanggan list reveals the same way.
- An AJAX endpoint or a second render path for `/Barang`: nothing re-renders the cards, so the partial is rendered once by `Index`.
- Counting the button into the title line: **N barang di buku** keeps reporting the whole list, not the revealed rows.
- Live search on `/Barang`: the search row stays the `aria-hidden` visual placeholder it is today.

## Verification

- [ ] `node --check wwwroot/custom/features/barang/index.js` passes.
- [ ] `dotnet build` succeeds.
- [ ] `/Barang` with more than eight items: the first eight cards are the only ones painted (`getComputedStyle` reads `none` for the rest), the button is present, one click paints eight more, and it removes itself on the last batch.
- [ ] `/Pelanggan` behaves the same after the added selector: eight visible, one click reveals eight more.
- [ ] `/Barang` with eight or fewer items shows every card and no button; the empty state is untouched.
- [ ] A revealed card still opens `/Barang/Details/{id}`; keyboard focus ring on the button is visible.
