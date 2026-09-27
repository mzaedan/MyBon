# Catat Bon Form Slicing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add `/Bon/Create` visual form from the Catat Bon Baru export and link the list CTA.

**Architecture:** `BonController.Create()` returns `View()`. Create markup reuses `.pelanggan-form`. Line card CSS is new. Index CTA becomes `a.cta-catat-bon`. `_Layout.cshtml` unchanged.

**Tech Stack:** ASP.NET Core MVC, Razor, Bootstrap Icons, `site.css`.

## Global Constraints

- No Tailwind CDN, no Phosphor paths, no database, no JavaScript totals or add/remove rows.
- GET Simpan re-renders empty Create; do not bind query values.
- Tambah barang and Hapus are visual `div`s with `aria-hidden="true"`.
- Do not commit unless the user asks.
- No test project; verify at `/Bon` and `/Bon/Create` plus `dotnet build`.

## File map

- Modify: `Controllers/BonController.cs`
- Modify: `Views/Bon/Index.cshtml`
- Create: `Views/Bon/Create.cshtml`
- Modify: `wwwroot/css/site.css`
- Unchanged: `Views/Shared/_Layout.cshtml`

---

### Task 1: Controller and markup

- [ ] Add `Create() => View()` on `BonController`.
- [ ] Index CTA: `<a class="cta-catat-bon" asp-controller="Bon" asp-action="Create">Catat bon baru</a>`
- [ ] Add `Views/Bon/Create.cshtml` per spec (fields, one line card, Batal/Simpan).

### Task 2: CSS

- [ ] Remove `.bon-cta`.
- [ ] Add field-with-icon, money input, daftar head, line card, qty, total row.

### Task 3: Verify

- [ ] `dotnet build`
- [ ] Browser: list CTA, Create title/back, fields, visual Tambah/Hapus, Simpan GET empty.
