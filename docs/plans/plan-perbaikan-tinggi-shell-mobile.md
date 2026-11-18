# Perbaikan tinggi shell di mobile (strip abu-abu di bawah layar)

**Goal:** On a phone in the mobile web view, the app did not fill the screen: a grey `#e5e7eb`
band sat along the bottom edge, under the header, the login column, and the bottom nav alike. The
shell has to be exactly one visible screen tall on every page, not only on Masuk.

**Source slice:** `docs/plans/plan-login-slicing.md` (the shell frame in `site.css`). It is the
same shell the other pages already inherit, so a fix here reaches every screen.

## Root cause

Two rules multiplied.

```css
:root {
  --safe-bottom: max(env(safe-area-inset-bottom, 0px), 48px);
}

.app-shell {
  height: 100vh;
  height: calc(100dvh - var(--safe-bottom));
}
```

`--safe-bottom` forced a floor of **48px** on top of whatever the device reports, as a stand-in
for the Android navigation bar. Two things are wrong with that:

- A CSS pixel is not a screen pixel on a phone. A 1080px-wide Android screen is about **412 CSS
  px** wide, so a 48px floor is far taller than the system bar it was meant to represent.
- `100dvh` **already excludes** the Android system bar and the desktop taskbar. It is the area the
  browser actually shows. Subtracting the bar height a second time can only ever cut the shell
  short.

So the shell ended `48px` above the bottom of the screen on every phone size, and `body.app-body`'s
grey `#e5e7eb` (the desktop canvas behind the card) painted in the gap. It looked worst on Masuk
because the login column ends in a rule right at the shell's edge.

Measured in headless Chrome with the phone user agent, viewport emulation, and the page's real
`site.css`:

| Viewport | `100dvh` | `.app-shell` height, before | Grey strip below |
|---|---|---|---|
| 360 × 640 | 640 | 592 | **48** |
| 390 × 844 | 844 | 796 | **48** |
| 412 × 915 | 915 | 867 | **48** |
| 430 × 932 | 932 | 884 | **48** |
| 480 × 800 | 800 | 752 | **48** |
| 767 × 700 | 700 | 652 | **48** |

## Change

```css
:root {
  /* env() only reports the notch and the iOS home indicator. 100dvh already excludes the system
     bar, so the inset is whatever the device itself reports and nothing more. */
  --safe-bottom: env(safe-area-inset-bottom, 0px);
}

.app-shell {
  height: 100vh;
  height: 100dvh;
}
```

That is the whole fix. Everything else already reads `--safe-bottom` correctly and keeps working
with the smaller value:

| Rule | Reads | With the old 48px floor | Now |
|---|---|---|---|
| `.bottom-nav` padding-bottom | `calc(8px + var(--safe-bottom))` | 56px | 8px (+ the real home indicator) |
| `.app-main` padding-bottom | `calc(var(--bottom-nav-h) + 8px)` | 124px | 76px (+ the real indicator) |
| `.auth-main` padding-bottom | `calc(24px + var(--safe-bottom))` | 72px | 24px (+ the real indicator) |

`--bottom-nav-h` was not changed: it still adds the device's own inset, so on a phone with a
gesture bar the last list row stays clear of it. Because the shell no longer subtracts that value,
the nav now sits on the shell's bottom edge instead of 48px above it.

Desktop is untouched: the `@media (min-width: 768px)` card keeps its own
`height: min(860px, calc(100dvh - 48px))`, 20px radius, border, and shadow. Its 48px is the 24px
page padding above and below the card, not a system bar, so it is correct and was left alone.

## Deliberately not done

- **No `--safe-area-inset-bottom` guess.** Removing the 48px floor means the fix does not depend on
  knowing the Android bar height at all. There is nothing to keep in sync with a device.
- **No `min-height: 100dvh` fallback or JS resize handler.** `dvh` is already supported by every
  browser that can run this app's `:has()` selectors (see
  `plan-perbaikan-gap-bawah-login.md`), and `100vh` stays as the first declaration for pre-`dvh`
  browsers.
- **No `body` change.** `#e5e7eb` stays the desktop canvas behind the card; on a phone the shell now
  covers it completely, which is the intent.
- **No per-layout override.** `.app-shell` is the one frame for `_Layout` and `_LayoutAuth`; a
  `.app-shell--auth` variant would have fixed Masuk and left Dashboard, Pelanggan, Barang, Bon, and
  Pengguna broken.
- **The 48px floor's original comment is corrected, not deleted.** It claimed the shell needed to
  reserve room for the system bar; the new comment records why that was wrong.

## File map

- Modified `wwwroot/css/site.css` (`--safe-bottom`, `.app-shell` height, comments)
- Create `docs/plans/plan-perbaikan-tinggi-shell-mobile.md` (this file)
- Modified `docs/plans/plan-login-slicing.md` (Updates entry)

No markup, controller, model, or JS change.

## Verification

- [x] `dotnet build --nologo`: 0 warnings, 0 errors.
- [x] Both stylesheets parse in the browser: 408 rules each, `.app-shell` resolves to a single
      `height` (`calc(100dvh - var(--safe-bottom))` before, `100dvh` after).
- [x] Real pages against the running app (headless Chrome, real phone user agent, request
      interception serving the working-tree `site.css`), for `/login`, `/Auth/GantiPassword`, and
      `/Auth/Ditolak` at 360, 390, 412, 430, 480, 767, 768, and 1280 wide:

| Viewport | Before: shell ends / grey below | After: shell ends / grey below |
|---|---|---|
| 360 × 640 | 592 / 48 | 640 / **0** |
| 390 × 844 | 796 / 48 | 844 / **0** |
| 412 × 915 | 867 / 48 | 915 / **0** |
| 430 × 932 | 884 / 48 | 932 / **0** |
| 480 × 800 | 752 / 48 | 800 / **0** |
| 767 × 700 | 652 / 48 | 700 / **0** |
| 768 × 800 (card) | 24 → 776 | 24 → 776 (unchanged) |
| 1280 × 900 (card) | 24 → 876 | 24 → 876 (unchanged) |

- [x] Same before/after comparison on the signed-in frame (`.app-shell` + `.app-header` +
      `.app-main` + `.bottom-nav`, the `_Layout` stack) at 360, 412, 430, 768, 1280:

| Viewport | Before: shell / grey below / nav bottom | After: shell / grey below / nav bottom |
|---|---|---|
| 360 × 640 | 0 → 592 / 48 / 592 | 0 → 640 / **0** / **640** |
| 412 × 915 | 0 → 867 / 48 / 867 | 0 → 915 / **0** / **915** |
| 430 × 932 | 0 → 884 / 48 / 884 | 0 → 932 / **0** / **932** |
| 768 × 800 | 24 → 776 / 24 / 775 | 24 → 776 / 24 / 775 (unchanged) |
| 1280 × 900 | 24 → 876 / 24 / 875 | 24 → 876 / 24 / 875 (unchanged) |

      The nav sits on the shell's bottom edge (`navOnShellEdge: 0`) instead of 48px above it.
- [x] No document scroll and no horizontal overflow at any of those sizes.
- [ ] Still needs an eye on a real Android phone: after the fix there should be no grey band under
      the header, the list, or the bottom nav.

## Known and unchanged

`plan-perbaikan-header-login.md` already gave `.auth-main` the
`calc(24px + var(--safe-bottom))` bottom padding; this change does not restore it, it only makes it
smaller, because the variable it reads is now honest. Likewise the desktop block never overrode
`--safe-bottom` — there was nothing to remove there.