# Perbaikan lebar shell pada halaman Masuk (mobile 390–480px)

**Goal:** On a phone wider than 390px — Pixel 9 Pro is 427px, iPhone Pro Max 430px — the white login shell stopped at 390px, so the grey `body` showed as gutters down both sides of the screen. The shell must fill the viewport on mobile and keep the framed card only on desktop.

**Source slice:** `docs/plans/plan-login-slicing.md` (Constraints: "Keep the shell frame from `site.css`: 390px max width, centered card on desktop"). The card was always meant to be the desktop presentation, but the width cap was not gated the same way as the decoration.

## Root cause

```css
.app-shell {
  width: 100%;
  max-width: var(--max-w); /* 390px */
}
```

Only the card treatment — border, 20px radius, shadow, fixed `min(860px, 100dvh - 48px)` height, and the `body`'s centering — sits behind `@media (min-width: 768px)`. The 390px cap applied at *every* width, so each viewport between 390px and 767px painted `body.app-body`'s `#e5e7eb` in the space the shell refused to take. 360–389px looked correct only because the shell was already filling the viewport there.

Measured in headless Chrome against the real page HTML (`wwwroot/css/site.css`, login markup, fonts loaded), before the fix:

| Viewport | `.app-shell` width | Grey gutters |
|---|---|---|
| 360 | 360 | none |
| 394 | 390 (capped) | 2px each side |
| 427 | 390 (capped) | 18.5px each side |
| 480 | 390 (capped) | 45px each side |

## File map

- Modify `wwwroot/css/site.css` (`.app-shell` mobile override, `.auth-page` column cap)
- Modify `docs/plans/plan-login-slicing.md` (update note)

No markup, controller, model, or JS change — the login view already renders inside the shell it is given.

## Change

```css
/* The 390px frame is the desktop presentation (the card below). A real phone is often wider
   than that — Pixel 9 Pro is 427px, iPhone Pro Max 430px — so on mobile the shell must fill the
   viewport instead of showing the grey body in gutters on both sides. */
@media (max-width: 767.98px) {
  .app-shell {
    max-width: none;
  }
}

.auth-page {
  /* Fluid on a phone, but the column stops growing at 480px so the fields and the button do not
     stretch across a wide phone/landscape viewport now that the shell is full width. */
  width: 100%;
  max-width: 480px;
  margin-right: auto;
  margin-left: auto;
}
```

`767.98px` pairs with the existing `768px` breakpoint, so the two blocks never overlap and nothing between 767px and 768px falls through. `--max-w: 390px` stays in `:root` — it is still the desktop frame width, which is where the framed card belongs.

The `.auth-page` cap only bites past 520px (480px column + 40px `.auth-main` gutter), so the 360–480px range keeps the shell's 20px side padding as the only inset, exactly like the 390px mockup. On a 767px-wide phone in landscape the column centers instead of stretching the fields edge to edge.

## Deliberately not done

- Not scoped to the auth layout with a `.app-shell--auth` modifier: the 390px cap mis-sized every screen on a modern phone, not only the login. The shell is one component and the frame is a desktop-only presentation, so the fix belongs there. `.app-main`, `.bottom-nav`, and the list pages inherit the correct full-bleed width with no other change.
- `--max-w` is not deleted or renamed, and the `@media (min-width: 768px)` card block is untouched — desktop still renders the 390px card, 20px radius, border and shadow.
- No change to the 20px `.auth-main` padding, the 24px column rhythm, the brand mark, the eye toggle, the checkbox, the forgot link, or the button. The design is the same, only the shell is no longer narrower than the screen.
- No `body` change: `#e5e7eb` stays the desktop canvas behind the card and is simply covered by the shell on mobile.
- No `overflow-x: hidden` added to `body`, no sticky footer, no new scroll container.

## Verification

- [x] `dotnet build --nologo`: 0 warnings, 0 errors; `GET /login` answers 200 and serves the updated `site.css`.
- [x] Layout metrics of the real markup + `site.css` at eight viewports (headless Chrome, iframes at exact CSS widths so media queries and `dvh` resolve against the tested width):

| Viewport | `.app-shell` w / x | `.auth-page` w | `documentElement.scrollWidth` vs `clientWidth` |
|---|---|---|---|
| 360 | 360 / 0 | 320 | 360 = 360 |
| 375 | 375 / 0 | 335 | 375 = 375 |
| 400 | 400 / 0 | 360 | 400 = 400 |
| 412 | 412 / 0 | 372 | 412 = 412 |
| 427 | 427 / 0 | 387 | 427 = 427 |
| 430 | 430 / 0 | 390 | 430 = 430 |
| 480 | 480 / 0 | 440 | 480 = 480 |
| 767 | 767 / 0 | 480 (centered, x=144) | 767 = 767 |

- [x] Screenshots of the live `/login` (temporary probe page, removed afterwards) at 427, 400, 360 and 480: white runs to both edges, no grey gutters, nothing clipped or overlapping, and the logo, title, tagline, description, fields, eye toggle, "Ingat saya", "Lupa kata sandi?" and "Masuk ke buku hutang" stay on the same 20px grid.
- [x] Desktop unchanged: at 1244px viewport `.app-shell` is still the 390px card (`x=427 w=390 h=700`, radius, border, shadow), and an accidental direct load of `/login` at 1750px showed the same centered card.
- [ ] Still needs an eye on a real 427px phone (Pixel 9 Pro) and on a 360px phone, portrait.

Known and unchanged: `.auth-forgot` still carries its `margin-right: -4px` so the label's text edge lines up with the `page-rule` edges; that makes `.auth-page.scrollWidth` read 4px over its `clientWidth`. The overhang sits inside `.auth-main`'s 20px side padding, so it is neither clipped nor scrollable — the document never gains a horizontal scrollbar (see the table above).
