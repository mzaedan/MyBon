# CatatHutang Cari Pelanggan (live search) design

Date: 2026-10-17  
App: ASP.NET Core MVC MyBon  
Depends on: Pelanggan list slice (`Views/Pelanggan/Index.cshtml`), `IPelangganService`, `BaseController`, `wwwroot/custom/features/pelanggan/index.js`

## Goal

Make the list search row real: type a name and the card list narrows as you type, without leaving `/Pelanggan`.

## Constraints (agreed)

- The row keeps the export look: `bi-search` icon, muted 15px text, rule underneath. It becomes a real `<input type="search">`.
- No new JS libraries: jQuery is already loaded by `_Layout.cshtml`; `index.js` keeps its plain ES5 style.
- Search matches the customer **name only**, case-insensitively; a phone number or an address typed in the box never matches.
- An empty keyword means "whole notebook", so clearing the box restores every card.
- Without scripting the row still works: the input sits in a `GET` form that reloads `/Pelanggan?q=...`.
- Out of scope: paging on the server, sorting options, remembering the last keyword, highlighting matches.

## Architecture

```
PelangganController.Index(string? q)   filtered first paint + ViewData["SearchQuery"]
PelangganController.Search(string? q)  AJAX -> PartialView("_Cards"), otherwise the list screen
IPelangganService.SearchAsync(keyword) ErrorOr<IReadOnlyList<Pelanggan>>
PelangganService.SearchAsync          ILIKE over nama only
Views/Pelanggan/_Cards.cshtml         cards + empty notice + "Muat nama berikutnya"
wwwroot/custom/features/pelanggan/index.js  debounce, AJAX, title count, reveal
```

`GetAllAsync` stays as the service's "everything" query; `SearchAsync` delegates to it when the keyword is blank.

## Behaviour

1. Typing debounces 250 ms, then `GET /Pelanggan/Search?q=<term>` with `X-Requested-With: XMLHttpRequest`.
2. The response is the same partial the first paint renders, and only `#pelanggan-card-stack` is replaced, so the input keeps focus and its text.
3. The count line reads **N nama cocok** while a keyword is active and **N nama di buku** when the box is empty; it hides when nothing matches.
4. No matches renders **Belum ada nama yang cocok. Coba kata kunci lain.** inside the stack, where the cards were.
5. **Muat nama berikutnya** still reveals eight more cards per click. It is rendered by the partial, and the click handler is delegated on the stack so it survives re-renders.
6. An in-flight request is aborted when a newer keystroke arrives; a failed request replaces the stack with **Gagal memuat hasil pencarian. Coba lagi.**
7. `Enter` submits through the script (no reload) when scripting is on, and through the form when it is not.

## Matching

`EF.Functions.ILike` on `Nama` with the term wrapped in `%`; `\`, `%`, and `_` typed by the user are escaped with a backslash escape character so they match literally. Only the name column is part of the predicate: a phone number or an address in the box matches nothing. Rows stay ordered by `CreatedAt` descending, like the unfiltered list.

## Styling

Appended next to the other pelanggan blocks in `site.css`, reusing tokens:

- `.pelanggan-search-input` — transparent, borderless 15px field filling the row; placeholder `--muted`, typed text `--text`.
- `.pelanggan-search-row:focus-within .page-rule` — the rule turns `--text` while the field has focus, because the bare input has no outline.
- `.pelanggan-card-stack` — column, `gap: 8px`, holds cards, empty notice, and the load-more button so the stack spacing is unchanged.
- `.pelanggan-search-empty` — 15px `--muted`, same as `.pelanggan-notice`.
- `.pelanggan-hidden` — `display: none`, toggled by the script on the count line.

## Data and errors

`SearchAsync` returns `ErrorOr<IReadOnlyList<Pelanggan>>` and follows the read path: no matches is a success with an empty list and the message **Tidak ada pelanggan yang cocok dengan "term"**. `Search` returns the `ErrorOr` payload as JSON to AJAX callers on failure, and 404/400/500 through `ToActionResult` for page requests.

## Verification

With `dotnet watch run` and the Postgres container up:

1. `/Pelanggan` lists every customer; typing `z` narrows the cards and the count line, typing `zzz` shows the empty notice.
2. Clearing the box restores every card and the **N nama di buku** count.
3. `/Pelanggan?q=Kp` paints the filtered list on first load with the term already in the box; with JavaScript disabled the same URL works through the form.
4. Name search is the only match: `z` finds the names starting with that letter, while a phone number (`0812345`) or an address (`Kp`) shows the empty notice.
5. **Muat nama berikutnya** still reveals eight more cards and disappears on the last batch.
6. A customer card still opens `/Pelanggan/Details/{id}`; delete and edit are unaffected.

## Non-goals

Server-side paging, search history, debounce tuning per device, match highlighting, DI of `IContext`, repository abstraction.
