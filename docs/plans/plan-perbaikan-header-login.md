# Perbaikan header pada halaman Masuk

**Goal:** The login header sits on the same line, in the same type, as the header on every other page. Today it starts 24px higher and inverts the type — small muted **MY BON** over a 22px **Masuk** — so the screen reads as a different app.

**Source slice:** `docs/plans/plan-login-slicing.md`. This change supersedes its Slice 2 Step 1 ("a small MY BON line over the Masuk title") and the `8px` top padding in Slice 3 Step 1.
**Spec:** `docs/superpowers/specs/2026-09-27-catahutang-shell-slicing-design.md` (Header section) — the shell header is wordmark + screen title. The login was the one screen that painted its own instead of reusing it.

## Root cause

`_LayoutAuth.cshtml` renders only `.app-shell > main.auth-main`; the view paints its own header with its own classes (`.auth-shop` 12px muted, `.auth-title` 22px black, 2px gap) inside `.auth-main`, whose top padding was 8px.

Every other page renders the shell header in `_Layout.cshtml`: `.app-header` (`padding-top: max(24px, calc(var(--safe-top) + 8px))`) + `.app-header-inner` (8px) + `.app-wordmark` (Fraunces 22px 600) + `.app-screen-title` (Figtree 13px `--muted`), 4px apart.

Measured in Chrome against the running app, viewport 1262×854 (shell 806px), offset from the top of the shell:

| Metric | Pelanggan (shell header) | Login before | Login after |
|---|---|---|---|
| wordmark top | 33 | 9 | 33 |
| wordmark type | Fraunces 22px 600 | Figtree 12px 400 | Fraunces 22px 600 |
| title top | 65 | 25 | 65 |
| title type | Figtree 13px rgb(92,92,92) | Figtree 22px black | Figtree 13px rgb(92,92,92) |
| title bottom | 80 | 52 | 80 |
| wordmark / title gap | 4px | 2px | 4px |
| first content top | 104 | 76 (rule) | 104 (rule) |

## File map

- Modify `Views/Auth/Login.cshtml` (header markup)
- Modify `wwwroot/css/site.css` (`.auth-main` top padding, `.auth-head` gap; delete `.auth-shop` and `.auth-title`)
- Modify `docs/plans/plan-login-slicing.md` (update note)
- Modify `docs/plans/plan-perbaikan-scrollbar-login.md` (re-measured column height)

## Change

```html
<header class="auth-head">
    <p class="app-wordmark">MY BON</p>
    <h1 class="app-screen-title">Masuk</h1>
</header>
```

```css
.auth-main {
  /* Mirrors the shell header offset (.app-header padding-top + .app-header-inner's 8px) so the
     login wordmark sits on the same line as the wordmark on every other page. */
  padding: calc(max(24px, calc(var(--safe-top) + 8px)) + 8px) 20px calc(24px + var(--safe-bottom));
}

.auth-head {
  /* Same 4px rhythm as .app-header-text, so the wordmark and the screen title line up with
     every other page. The type itself comes from .app-wordmark and .app-screen-title. */
  gap: 4px;
}
```

Reusing the two shell classes is the point: the login header cannot drift from the shell header again. `.auth-shop` and `.auth-title` are deleted — the login was their only user. The 20px side padding already matched both the shell header and `.app-main`, so the left edge lines up too, and the 24px `.auth-page` gap down to the first `.page-rule` matches `.app-main`'s 24px top padding, so the first content line lands on 104px on both.

## Deliberately not done

- The header does not move into `_LayoutAuth.cshtml` / `.app-header`: the slice keeps the auth shell free of the app header because the login is not a bottom-nav page. The view keeps painting the header, it just uses the shell's classes now.
- The header is not `sticky`. `.app-header` sticks above the scroller; here it scrolls with the column. Identical at rest, and a sticky header inside `.auth-main` would need its own background to hide the rule and content sliding under it.
- `.app-wordmark` / `.app-screen-title` are untouched: they stay the shell's classes and the login only reuses them.
- No change to the brand mark, the fields, the buttons, or the closing note.
- **Masuk** stays the page `<h1>`, the way `_Layout.cshtml` makes the screen title the `<h1>` and the wordmark a `<p>`.

## Verification

- [x] `dotnet build --nologo`: 0 warnings, 0 errors.
- [x] Chrome harness against the real page HTML (temporary copies outside the repo, removed afterwards) at viewport 1262×854 and 1262×604, login side by side with the real `/Pelanggan`:

| Metric | Pelanggan | Login |
|---|---|---|
| wordmark top / type | 33 / Fraunces 22px 600 | 33 / Fraunces 22px 600 |
| title top / bottom / type | 65 / 80 / Figtree 13px rgb(92,92,92) | 65 / 80 / Figtree 13px rgb(92,92,92) |
| wordmark–title gap | 4px | 4px |
| first content top | 104 | 104 |
| frame 556px: `main.scrollHeight` vs `clientHeight` | 1181 > 475 (bar visible) | 726 > 554 (bar hidden) |
| `main.clientWidth` vs `offsetWidth` | 373 vs 388 | 388 vs 388 |

- [x] Screenshots of the live `/login` at 1280×950 and 1280×700: header matches `/Pelanggan`, no scrollbar on the frame edge.
- [ ] Still needs an eye on a real phone: the wordmark starts below the notch (`--safe-top`), exactly like the shell header.
