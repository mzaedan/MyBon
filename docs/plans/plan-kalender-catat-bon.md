# Kalender Catat Bon plan

**Goal:** The **Tanggal** row on `/Bon/Create` stops using the browser's native date picker. It becomes the trigger row and calendar popup the **Catat pembayaran** sheet on `/Bon/Details` already uses, so both forms share one date control.

**Source:** `docs/plans/plan-kalender-catat-pembayaran.md` (the popup this slice reuses).
**Depends on:** `Views/Bon/Create.cshtml`, `Views/Bon/Details.cshtml`, `wwwroot/custom/features/bon/create.js`, `wwwroot/custom/features/bon/details.js`, `.bon-date-*` / `.bon-cal-*` in `wwwroot/css/site.css`.

**Status:** implemented on 2026-10-31 and verified in headless Chrome against the live page. The row writes the ISO date into the hidden `Tanggal` field the form posts; the server still validates it as required.

## Architecture

The calendar controller left `details.js` for `wwwroot/helpers/bon-calendar.js`, because two features paint it now. `window.BonCalendar.create(options)` takes the ids each page painted and returns `{ close, isOpen, popup }`, so the Catat pembayaran sheet keeps treating the popup as its focus scope.

`Details` keeps its markup inside `#bonPaySheet`: the sheet travels to `.app-shell` and carries the calendar with it. `Create` paints `#bonTanggalCalendar` at the end of the page and hands it to the shell with `host: shell`; otherwise it would scroll away inside `.app-main`. `.bon-cal--page` lifts the standalone overlay to the sheet layer (`z-index: 60`) so it covers the header and the bottom nav.

No new library: the grid is still built with the DOM API, and Bootstrap Icons keep `bi-calendar`, `bi-chevron-left`, and `bi-chevron-right`.

```
Views/Bon/Create.cshtml                  trigger row + hidden Tanggal + #bonTanggalCalendar
Views/Bon/Details.cshtml                 unchanged markup; loads the shared helper
wwwroot/helpers/bon-calendar.js          create(options) -> { close, isOpen, popup } (new)
wwwroot/custom/features/bon/create.js    initTanggalCalendar(shell)
wwwroot/custom/features/bon/details.js   delegates its Tanggal row to the helper
wwwroot/css/site.css                     drops the native .bon-date rules, adds .bon-cal--page
```

## File map

- Create: `docs/plans/plan-kalender-catat-bon.md`
- Create: `wwwroot/helpers/bon-calendar.js`
- Modify: `Views/Bon/Create.cshtml`, `Views/Bon/Details.cshtml`
- Modify: `wwwroot/custom/features/bon/create.js`, `wwwroot/custom/features/bon/details.js`
- Modify: `wwwroot/css/site.css`
- Modify: `docs/plans/plan-kalender-catat-pembayaran.md` (points at the shared helper)

## Slice 1: shared calendar

- [x] **Step 1:** Move `MONTHS`, `parseIso`, `toIso`, `startOfMonth`, `addDays`, `addMonths`, `sameDay`, `formatTanggal`, and the popup controller out of `details.js` into `wwwroot/helpers/bon-calendar.js` as `window.BonCalendar.create(options)`.
- [x] **Step 2:** Options are `overlayId`, `triggerId`, `valueId`, `inputId`, `titleId`, `gridId`, and an optional `host`; the helper returns `null` when a painted element is missing.
- [x] **Step 3:** Add the Tab trap to the popup so it holds focus when no sheet surrounds it; Escape still closes only the popup.
- [x] **Step 4:** `details.js` calls the helper with its existing ids and keeps `calendar.close(false)` / `calendar.popup` for the sheet controller.

## Slice 2: Create row

- [x] **Step 1:** Replace `<input asp-for="Tanggal" type="date" class="pelanggan-field-input bon-date">` with a hidden ISO field (`id="tanggal"`, `name="Tanggal"`) and a `bon-picker-trigger bon-date-trigger` button (`#tanggalBtn`) that reads **31 Oktober 2026**, or **Pilih tanggal** in muted type when empty.
- [x] **Step 2:** Swap the `<label for="tanggal">` for `<span class="pelanggan-field-label" id="tanggalLabel">` so the button can read `aria-labelledby="tanggalLabel tanggalValue"`; keep `asp-validation-for="Tanggal"`.
- [x] **Step 3:** Paint `#bonTanggalCalendar` after the customer sheet: scrim, popup, header with prev/next, `Sen`..`Min`, an empty `#bonTanggalCalGrid`, **Clear**, and **Hari ini**.
- [x] **Step 4:** `create.js` calls `initTanggalCalendar(shell)` with `host: shell`; the row paints the Indonesian date and the hidden field keeps ISO for the POST.
- [x] **Step 5:** `Views/Bon/Create.cshtml` and `Views/Bon/Details.cshtml` load `~/helpers/bon-calendar.js` before their feature script.

## Slice 3: styling

- [x] **Step 1:** Delete the native `.bon-date` and `.bon-date::-webkit-calendar-picker-indicator` rules.
- [x] **Step 2:** Note that `.bon-date-trigger` / `.bon-date-icon` are shared by both pages.
- [x] **Step 3:** `.bon-cal--page { z-index: 60 }` for the standalone overlay on Create.

## Verification

Checked with `dotnet build`, `node --check`, a live `GET /Bon/Create`, a rejected `POST /Bon/Create`, and headless Chrome over the Chrome DevTools Protocol:

- [x] `node --check` passes for the helper, `create.js`, and `details.js`.
- [x] `dotnet build` succeeds: 0 warnings, 0 errors.
- [x] `/Bon/Create` returns 200 with `#tanggalBtn`, a hidden `Tanggal=2026-10-31`, the row reading **31 Oktober 2026**, `#bonTanggalCalendar`, and the helper script tag.
- [x] A rejected POST (over-long `TotalBayar`) repaints `Tanggal=2026-10-15` and **15 Oktober 2026**, so the hidden field binds.
- [x] Create: the popup opens on **Oktober 2026** with 42 day cells, day 31 selected and focused; picking 15 writes `2026-10-15`, closes the popup, and returns focus to the row.
- [x] Create: **Clear** empties the field and shows **Pilih tanggal**; **next** shows **November 2026**; **Hari ini** writes today; Escape closes the popup.
- [x] Create: the overlay is a child of `.app-shell`, and the page raises no JavaScript exception.
- [x] Details: the payment sheet opens, its calendar opens, and picking 14 still writes `2026-10-14` while the sheet stays open.
- [x] Computed styles match the Details row: Figtree 16px/600, `min-height: 0`, `padding: 0 24px 0 0`, accent `#1877F2` calendar icon, 73.39px row height.

Still open:

- [ ] A human visual pass over `/Bon/Create` for pixel spacing against the export frame.

## Non-goals

Min/max dates, disabled days, month and year dropdowns, a native date fallback, time zones, a new JS library, Tailwind, Phosphor icon paths, changing the `bon` table, or changing how `Tanggal` is validated.

