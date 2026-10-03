# Detail Catat Bon Slicing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `/Bon/Details`, a static read-only bon sheet from the export, and link each Bon list card to it.

**Architecture:** `_Layout.cshtml` stays unchanged (back already exists). `BonController` gains `Details() => View()`. New `Views/Bon/Details.cshtml` holds the sheet. `Views/Bon/Index.cshtml` cards become anchors. New classes live in `wwwroot/css/site.css`.

**Tech Stack:** ASP.NET Core MVC, Razor, Bootstrap Icons, `site.css`.

## Global Constraints

- No Tailwind CDN.
- No models, services, database, or query string.
- Detail rows are read-only text, not inputs.
- **Catat pembayaran** is `<a>` to `Bon/Index`; **Tambah barang** is `<a>` to `Bon/Create`. No dead controls.
- Back uses `ViewData["BackHref"] = Url.Action("Index", "Bon")`.
- Do not change `_Layout.cshtml` or bottom nav.
- Do not restyle Dashboard, Barang, Pelanggan, Privacy, or Error.
- Do not commit unless the user asks.
- No test project; verify at `/Bon` and `/Bon/Details` and with `dotnet build`.

## File map

- Modify: `Controllers/BonController.cs` (add `Details()`)
- Modify: `Views/Bon/Index.cshtml` (`div.bon-card` -> `a.bon-card`)
- Create: `Views/Bon/Details.cshtml`
- Modify: `wwwroot/css/site.css` (add `a.bon-card`, `.bon-detail-*` rules after the Bon-form block)
- Unchanged: `Views/Shared/_Layout.cshtml`

---

### Task 1: Controller action

**Files:**
- Modify: `Controllers/BonController.cs`

- [ ] **Step 1:** Add `public IActionResult Details() => View();` next to `Create()`.

---

### Task 2: Details markup

**Files:**
- Create: `Views/Bon/Details.cshtml`

**Interfaces:**
- Consumes: layout `ViewData["Title"]`, `ViewData["BackHref"]`, `.page-rule`
- Produces: rule, bon meta, Ringkasan card, actions row, Riwayat list, Daftar barang list + total footer

- [ ] **Step 1:** Write the view with the BN-014 fixture values from the spec.
- [ ] **Step 2:** Confirm no `<input>`, `<form>`, or dead `href="#")`.

---

### Task 3: List cards become links

**Files:**
- Modify: `Views/Bon/Index.cshtml`

- [ ] **Step 1:** Change each `<div class="bon-card">...</div>` to `<a class="bon-card" asp-controller="Bon" asp-action="Details">...</a>`.
- [ ] **Step 2:** Keep inner markup and the `.bon-cta` -> CTA link unchanged.

---

### Task 4: CSS

**Files:**
- Modify: `wwwroot/css/site.css`

**Interfaces:**
- Consumes: `--muted`, `--text`, `--accent`, `--border`, `--surface`, `--font`, `--font-display`
- Produces: `a.bon-card`, `.bon-detail-page`, `.bon-detail-meta`, `.bon-summary*`, `.bon-action*`, `.bon-history*`, `.bon-item*`

- [ ] **Step 1:** Add `a.bon-card` mirroring `a.pelanggan-card` (color inherit, no underline, pointer, focus ring).
- [ ] **Step 2:** Add the detail-sheet classes.

---

### Task 5: Verify

- [ ] **Step 1:** Run `dotnet build`. Expected: success.
- [ ] **Step 2:** Open `/Bon/Details`: title BN-014, back chevron, active Catat Bon tab, meta, Ringkasan, two actions, Riwayat, Daftar barang + total.
- [ ] **Step 3:** Open `/Bon`, click any card -> Details. Click **Catat pembayaran** -> `/Bon`; **Tambah barang** -> `/Bon/Create`.

Do not commit.
