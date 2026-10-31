# Kalender Catat pembayaran slicing plan

**Goal:** On `/Bon/Details`, the **Tanggal** row inside the **Catat pembayaran** sheet opens the calendar popup from `CatatHutang-export.html` (`data-pencil-name="Kalender Popup"`): month header with prev/next, a Monday-first day grid, **Clear**, and **Hari ini**. Picking a day writes the date back to the row.

**Source:** `CatatHutang-export.html` (frame `Kalender Popup`, layered over the `Catat pembayaran` sheet).
**Depends on:** `Views/Bon/Details.cshtml`, `wwwroot/custom/features/bon/details.js`, `.bon-pay*` and `.bon-picker-trigger` in `wwwroot/css/site.css`, `docs/plans/plan-catat-pembayaran-slicing.md`.

**Status:** visual slice only. No model, service, database, or POST action. The pick writes the ISO date into the hidden `tanggal` field and formats the row; recording the payment still waits for `docs/plans/plan-bon-dinamis.md` (Follow-ups: *Record a payment from Details*).

## Architecture

The native `<input type="date">` cannot carry the export (Fraunces day cells, month header, Clear/Hari ini), so the row becomes a trigger button plus a hidden input, exactly like **Pelanggan** on `Create.cshtml`. The popup is a second overlay inside `#bonPaySheet`, so it travels with the sheet when the feature script moves that overlay onto `.app-shell`. Its scrim covers the sheet (`#0000004D`) and the card is anchored above the Tanggal row by the script, clamped inside the shell.

The grid is built with the DOM API: no new JS library, no Tailwind, no Phosphor paths. Bootstrap Icons (`bi-chevron-left`, `bi-chevron-right`, `bi-calendar`) follow the rest of the app.

```
BonController.Details()                  unchanged (GET, View())
Views/Bon/Details.cshtml                 date trigger + hidden field + #bonPayCalendar
wwwroot/custom/features/bon/details.js   calendar controller inside the existing sheet controller
wwwroot/css/site.css                     .bon-date-* row + .bon-cal-* popup
```

## File map

- Create: `docs/plans/plan-kalender-catat-pembayaran.md`
- Modify: `Views/Bon/Details.cshtml`
- Modify: `wwwroot/custom/features/bon/details.js`
- Modify: `wwwroot/css/site.css`
- Modify: `docs/plans/plan-catat-pembayaran-slicing.md` (its native date note is superseded here)

## Slice 1: trigger row

- [x] **Step 1:** Replace the native date input with `<input id="bonPayTanggal" type="hidden" name="tanggal" value="2026-10-25">` and `<button type="button" id="bonPayTanggalBtn" class="bon-picker-trigger bon-date-trigger" aria-haspopup="dialog" aria-expanded="false" aria-controls="bonPayCalendar">`.
- [x] **Step 2:** Swap the `<label>` for `<span class="pelanggan-field-label" id="bonPayTanggalLabel">` so the button can read `aria-labelledby="bonPayTanggalLabel bonPayTanggalValue"`; the visible value lives in `#bonPayTanggalValue`.
- [x] **Step 3:** Keep the `bon-field-with-icon` wrapper and give the `bi-calendar` icon `.bon-date-icon` (accent blue, 16px) like the export.

## Slice 2: popup markup

- [x] **Step 1:** Add `#bonPayCalendar` after `.bon-pay-sheet`, `hidden` by default: `.bon-cal-scrim` (`data-bon-cal-close`) and `.bon-cal-popup` (`role="dialog"`, `aria-modal="true"`, `aria-labelledby="bonCalTitle"`, `tabindex="-1"`).
- [x] **Step 2:** Header: previous button (`bi-chevron-left`, `data-bon-cal-prev`), `#bonCalTitle` with `aria-live="polite"`, next button (`bi-chevron-right`, `data-bon-cal-next`).
- [x] **Step 3:** Weekday row **Sen Sel Rab Kam Jum Sab Min** (11px muted), and an empty `#bonCalGrid` the script fills with six `.bon-cal-week` rows.
- [x] **Step 4:** Actions row: **Clear** (`data-bon-cal-clear`) and **Hari ini** (`data-bon-cal-today`).

## Slice 3: behaviour

- [x] **Step 1:** `details.js` parses the hidden ISO value, formats the row as `25 Oktober 2026`, and shows **Pilih tanggal** in muted type when empty.
- [x] **Step 2:** Opening the popup renders the selected month (Monday-first, six weeks, muted outside days, accent **today**, accent-filled **selected** day), unhides the overlay, sets `aria-expanded`, anchors the card above the Tanggal row, and focuses the selected day.
- [x] **Step 3:** Prev/next move one month and keep focus on the selected day (or the 1st); arrow keys move between day cells; a day pick, **Clear**, and **Hari ini** update the hidden field, render the row, close the popup, and return focus to the trigger.
- [x] **Step 4:** Escape and the popup scrim close only the calendar; the sheet's Escape still closes the sheet. Tab/Shift+Tab stay inside the popup while it is open.
- [x] **Step 5:** Closing the pay sheet also closes the calendar without stealing focus from the restored trigger.

## Slice 4: styling

- [x] **Step 1:** `.bon-date-trigger` keeps the form row compact (`min-height: 0`, Figtree 16px semibold); `.bon-date-icon` is accent blue.
- [x] **Step 2:** `.bon-cal`, `.bon-cal.is-open`, `.bon-cal-scrim` (`#0000004D`), `.bon-cal-popup` (20px side insets, 16px padding, 16px radius, `0 8px 28px #00000026`, gap 8px, scrolls on short shells).
- [x] **Step 3:** `.bon-cal-head`, `.bon-cal-title` (16px semibold), `.bon-cal-nav` (40px, 20px icon), `.bon-cal-weekdays`/`.bon-cal-week` (7 equal columns, 2px gap), `.bon-cal-day` (44px tall, 22px radius, Fraunces 15px), `.is-outside` `#767676`, `.is-today` accent text, `.is-selected` accent fill + white semibold.
- [x] **Step 4:** `.bon-cal-actions`, `.bon-cal-clear` (muted 15px semibold, 44px), `.bon-cal-today` (`12px 16px`, `#E5E5E5` outline, 8px radius), plus shared `:focus-visible` rings.

## Verification

Checked with `dotnet build`, `node --check`, and a live `GET /Bon/Details`:

- [x] `node --check wwwroot/custom/features/bon/details.js` passes.
- [x] `dotnet build` succeeds: 0 warnings, 0 errors.
- [x] `/Bon/Details` returns 200 with `#bonPayTanggalBtn`, `#bonPayTanggal`, `#bonPayCalendar`, `#bonCalTitle`, and an empty `#bonCalGrid` (the script fills it).
- [x] The row reads **25 Oktober 2026**; the hidden field holds `2026-10-25`.
- [x] Opening the popup renders **Oktober 2026**, seven weekday labels, six week rows (42 day buttons), day 25 accent-filled and labelled `25 Oktober 2026`, days before 1 October muted.
- [x] Prev shows **September 2026**; next returns to **Oktober 2026**.
- [x] Picking 14 writes `2026-10-14`, the row reads **14 Oktober 2026**, and focus returns to the trigger.
- [x] **Hari ini** writes today's ISO date; **Clear** empties the hidden field and shows **Pilih tanggal** in muted type.
- [x] Escape closes only the calendar; a second Escape closes the pay sheet and restores focus to **Catat pembayaran**.
- [x] Tab/Shift+Tab and arrow keys stay inside the calendar while it is open.
- [x] Computed style matches the export where checked: popup radius 16px, scrim `rgba(0, 0, 0, 0.3)`, day cells 44px / Fraunces 15px, selected `#1877F2`.

Still open:

- [ ] A human visual pass over `/Bon/Details` for pixel spacing against the export frame.

## Non-goals

Min/max dates, disabled days, year and month dropdowns, a native date fallback, payment persistence, `bon_payment` rows, validation, server-side dates, a new JS library, Tailwind, Phosphor icon paths.

> Update (2026-10-31): the controller now lives in `wwwroot/helpers/bon-calendar.js` so `/Bon/Create` can open the same popup. `details.js` calls `BonCalendar.create(...)`; the ids and behaviour above are unchanged. See `docs/plans/plan-kalender-catat-bon.md`.

> Update (2026-10-31): the sheet this calendar belongs to now saves. The hidden `#bonPayTanggal` posts as `Tanggal` on `#bonPayForm` to `POST /Bon/AddPayment`, and a cleared date is refused with **Tanggal wajib diisi**. See `docs/plans/plan-catat-pembayaran-dinamis.md`.

