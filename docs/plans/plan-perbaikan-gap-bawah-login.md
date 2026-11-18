# Perbaikan strip kosong di bawah halaman Masuk

**Goal:** On the login screen (`/login`, `_LayoutAuth`) an unwanted empty area showed under **Masuk ke buku hutang**: a large white gap, a thin rule, then a light-grey strip at the very bottom of the frame. That strip was the `body.app-body` colour leaking out under the login column, not a footer.

**Source slice:** `docs/plans/plan-login-slicing.md` (Slice 3 Step 1: "`.auth-page` is a 24px column whose spacer grows").

## Root cause

```css
.auth-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.auth-page > .auth-spacer + .page-rule {
  margin-bottom: 0; /* rule ends the column */
}
```

The rule's `margin-bottom` was zeroed, but `gap: 24px` measures the space *between* flex items, so the column still reserved a further 24px after the closing `<hr class="page-rule">`. `.auth-page` sits inside `.auth-main`, which has `overflow: auto`, so the column's `flex: 1 0 auto` did not grow into the viewport's remaining height: the spacer stayed at its 24px minimum and the leftover space (plus the trailing gap) painted the shell background, which is the grey `#e5e7eb` canvas showing through under the white card, i.e. the strip.

## Change

```css
/* `.auth-actions` is the only sibling after the rule, so the column's own 24px row gap has to go too. */
.auth-page:has(> .auth-spacer + .page-rule) {
  row-gap: 0;
}

.auth-page > .auth-spacer + .page-rule {
  margin-bottom: 0;
}
```

`:has()` is already relied on in this stylesheet (`.pelanggan-field:has(.input-validation-error)`), so no new baseline is needed. The gap stays for any auth view that has no closing rule.

## File map

- Modified `wwwroot/css/site.css` (`.auth-page:has(...)` rule)
- Modified `docs/plans/plan-login-slicing.md` (Updates)

No markup, layout, controller, or JS change.

## Verification

- [x] `dotnet build --nologo`: 0 warnings, 0 errors; `GET /login` still 200.
- [x] Headless measurement of the live page: `.auth-page` bottom `645.6` == closing rule bottom `645.6` (gap below rule `0`), `row-gap: 0px`, and the rule (`--border` `#e5e5e5`) now ends flush on the shell's bottom edge.
- [x] Desktop screenshot: the card closes with its own white surface and the rounded bottom corner, no grey bar inside the frame.