# Perbaikan focus rule pada cari Pelanggan

**Goal:** The search row on `/Pelanggan` darkens its rule while the field has focus, the way `/Barang` already does. Today the rule never changes, so the two lists look different at the one moment the user is typing.

**Spec:** `docs/superpowers/specs/2026-10-17-cari-pelanggan-live-search-design.md` (Styling section)

**Found while checking:** `docs/plans/plan-barang-cari.md` ("Deliberately not done") — the Barang slice noticed the Pelanggan focus rule never matched and left it for a separate change. This is that change.

## Root cause

```html
<form class="pelanggan-search">
    <div class="pelanggan-search-row">
        <i class="bi bi-search pelanggan-search-icon"></i>
        <input class="pelanggan-search-input" ... />
    </div>
    <hr class="page-rule" />   <!-- sibling of the row, not a descendant -->
</form>
```

`hr.page-rule` is the row's **sibling** inside the form, so the descendant selector `.pelanggan-search-row:focus-within .page-rule` can never match — the form has to be the anchor, the way `.barang-search:focus-within .page-rule` is.

## File map

- Modify: `wwwroot/css/site.css` (focus rule selector + comment)
- Modify: `docs/superpowers/specs/2026-10-17-cari-pelanggan-live-search-design.md` (stale selector in the Styling section)
- Modify: `docs/plans/plan-barang-cari.md` ("Deliberately not done" note now points here)

## Change

```css
/* The rule under the row carries the focus state; the bare input has no outline of its own.
   Both forms anchor on the form: the rule is the row's sibling, not a descendant of it, so a
   `.pelanggan-search-row:focus-within .page-rule` selector would never match. */
.pelanggan-search:focus-within .page-rule,
.barang-search:focus-within .page-rule {
  background: var(--text);
}
```

The two rules were identical, so they are one group now, like the other shared `.pelanggan-search` / `.barang-search` blocks in the same file.

## Deliberately not done

- No markup change: the form is already the right anchor; only the selector was wrong.
- No JS change: the input still has no outline of its own, and the script does not touch `page-rule`.
- Not touching the sheets (`.bon-picker-search-row`) or the still-static `/Bon` row.

## Verification

`node`/Chrome harness loading the real `site.css` plus the real markup of both rows (temporary file outside the repo, removed afterwards), reading `getComputedStyle(rule).backgroundColor` with the input focused and blurred:

| State | Before | After |
|---|---|---|
| `.barang-search` focused | `rgb(0, 0, 0)` | `rgb(0, 0, 0)` |
| `.pelanggan-search` focused | `rgb(229, 229, 229)` | `rgb(0, 0, 0)` |
| `.pelanggan-search` blurred | `rgb(229, 229, 229)` | `rgb(229, 229, 229)` |

Tokens in the same run: `--text=#000000`, `--border=#E5E5E5`. Row metrics are unchanged by the fix — `display: flex`, `align-items: center`, `gap: 8px`, `padding: 0`, height 17px for both rows; icon 18px `--muted`; input 15px, borderless, transparent; rule 1px, no margin.

- [x] `dotnet build --nologo`: 0 warnings, 0 errors.
- [ ] Still needs a browser: Tab into the box on `/Pelanggan`, the rule darkens, blur restores it, and typing still narrows the cards.
