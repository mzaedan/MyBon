# Catat Bon List Slicing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Bon placeholder with a static Catat Bon list slice from the export.

**Architecture:** `_Layout.cshtml` stays unchanged. `Views/Bon/Index.cshtml` holds hardcoded search, count, four cards, and a visual-only CTA `div`. New classes live in `wwwroot/css/site.css`. `BonController` stays `Index() => View()`.

**Tech Stack:** ASP.NET Core MVC, Razor, Bootstrap Icons, `site.css`.

## Global Constraints

- No Tailwind CDN.
- No models, services, database, or JavaScript filter.
- Search is not an `<input>` or `<form>`.
- Cards are `div`s with no `href`.
- **Catat bon baru** is `<div class="bon-cta" aria-hidden="true">`, not `<a>` or `<button>`. Do not use `.cta-catat-bon` on it.
- Do not add `Bon/Create` or `ViewData["BackHref"]`.
- Do not change `_Layout.cshtml` or bottom nav.
- Do not commit unless the user asks.
- No test project; verify at `/Bon` and with `dotnet build`.

## File map

- Modify: `Views/Bon/Index.cshtml`
- Modify: `wwwroot/css/site.css` (group `.bon-search*` with existing search selectors; add card/CTA rules after `.barang-card-price`)
- Unchanged: `Controllers/BonController.cs`, `Views/Shared/_Layout.cshtml`

---

### Task 1: Bon markup

**Files:**
- Modify: `Views/Bon/Index.cshtml`

**Interfaces:**
- Consumes: layout `ViewData["Title"]`, `.page-rule`
- Produces: static Bon page body with four dummy rows from the spec table

- [ ] **Step 1:** Replace the placeholder with rule, visual search **Cari nomor atau nama**, **4 bon di buku**, four cards, visual CTA.

- [ ] **Step 2:** Confirm no `<input>`, `<form>`, card `href`, or CTA `a`/`button`.

---

### Task 2: Bon CSS and verify

**Files:**
- Modify: `wwwroot/css/site.css`

**Interfaces:**
- Consumes: `--muted`, `--text`, `--border`, `--surface`, `--accent`, `--font`, `--font-display`
- Produces: `.bon-page`, grouped search/title selectors, `.bon-cards`, `.bon-card`, status modifier, amounts, `.bon-cta`

- [ ] **Step 1:** Add classes. `.bon-cta` copies CTA size/color but `cursor: default`, no `:active`, no focus ring.

- [ ] **Step 2:** Run `dotnet build`. Expected: success.

- [ ] **Step 3:** Open `/Bon`: title, active tab, four cards, Lunas muted, CTA and cards do not navigate.

Do not commit.
