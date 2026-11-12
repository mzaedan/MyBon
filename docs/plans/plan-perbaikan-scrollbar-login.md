# Perbaikan scrollbar pada halaman Masuk

**Goal:** The login screen paints its own frame with no scrollbar on the right edge. Today a classic desktop scrollbar sits on the edge of the phone shell whenever the window is shorter than the login column.

**Source slice:** `docs/plans/plan-login-slicing.md` (`.auth-main` owns the scroll).
**File touched:** `wwwroot/css/site.css` only. No markup, controller, model, or JS change.

## Root cause

`.app-shell` is a fixed phone frame — `overflow: hidden`, and on desktop `height: min(860px, calc(100dvh - 48px))`. `.auth-main` is the only scroller inside it (`overflow-y: auto`), by design. The login column is ~726px tall end to end (markup + 32px top + 24px bottom padding, measured in Chrome; 698px before the header alignment in `plan-perbaikan-header-login.md`), so on any window shorter than roughly 774px the column outgrows the frame and the browser paints a native scrollbar over the frame's right edge. On a phone there is no such bar (overlay scrollbars), so the slice looked right on the mockup device and wrong on a laptop.

Measured on the real page (`dotnet run` + Chrome headless), before the fix:

| Viewport | Frame height | `main.scrollHeight` vs `clientHeight` | Edge scrollbar |
|---|---|---|---|
| 1262 × 604 | 556 | 726 > 554 | yes |
| 1262 × 854 | 806 | 804 = 804 | no |
| 504 × 604 (below 768px: no desktop frame) | 604 | 726 > 604 | yes |

## Change

```css
.auth-main {
  /* ...existing flex/overflow rules... */
  /* The shell is a fixed phone frame, so the login column can outgrow it on a short
     window. The scroll stays (wheel, touch, keyboard) but the classic desktop
     scrollbar is dropped: it would paint over the frame's right edge and shift the
     column by its width. */
  scrollbar-width: none;
  -ms-overflow-style: none;
}

.auth-main::-webkit-scrollbar {
  width: 0;
  height: 0;
}
```

`scrollbar-width` covers Chrome 121+, Firefox and Edge; the `::-webkit-scrollbar` block keeps Safari, older Chrome and the legacy WebKit engines on the same result. The element keeps `overflow-y: auto`, so a genuinely short window can still reach the closing note.

## Deliberately not done

- No markup change: the slice already puts the scroll on `.auth-main`, which the plan asks for.
- No shell change: `.app-shell` keeps its fixed 390px × 860px frame for every screen, and `.app-main` on the other pages keeps its visible scrollbar — that one belongs to the list screens, this fix is the login frame only.
- No new scroll container, no `scrollbar-gutter`, no sticky footer: the flexible `.auth-spacer` still holds the note at the bottom on a tall frame.
- No content shrink (`gap`, brand size, or a `@media (max-height)` pass): the 24px rhythm from the mockup stays.

## Verification

- [x] `dotnet build` succeeds; the served `wwwroot/css/site.css` carries `scrollbar-width: none` (line 2475) and `.auth-main::-webkit-scrollbar` (line 2479).
- [x] `GET /login` answers 200 and the page still swaps to the app stylesheet with a fresh `asp-append-version` hash.
- [x] Chrome screenshot of the real `/login` at 1280 × 700 (frame 556px, the case that used to fail): the bar is gone, the frame edge is clean, the column is no longer narrowed by the bar width.

| Viewport | `main.scrollHeight` vs `clientHeight` | `scrollbar-width` | `clientWidth` vs `offsetWidth` | Edge scrollbar |
|---|---|---|---|---|
| 1262 × 604 | 726 > 554 (still scrollable) | none | 388 = 388 | no |
| 1262 × 854 | 804 = 804 | none | 388 = 388 | no |
| 504 × 748 | 748 = 748 | none | 390 = 390 | no |

Re-measured after the header alignment (`plan-perbaikan-header-login.md`), which added 28px to the column: the overflow it needs to keep scrolling for grew, the bar stayed gone.

- [ ] Still needs a hand on a real laptop: wheel-scroll the login frame in a short window to confirm the closing note is reachable without the bar.
