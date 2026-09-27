# Pelanggan List Slicing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the Pelanggan placeholder with a static slice of the CatatHutang export list.

**Architecture:** `_Layout.cshtml` stays unchanged. `Views/Pelanggan/Index.cshtml` holds hardcoded search row, count, ten cards, and a visual-only CTA. New classes live in `wwwroot/css/site.css`. `PelangganController` is unchanged.

**Tech Stack:** ASP.NET Core MVC, Razor, Bootstrap Icons, `site.css`.

## Global Constraints

- No Tailwind CDN.
- No models, services, database, or JavaScript filter.
- Search is not an `<input>` or `<form>`.
- Cards are `div`s with no `href`.
- **Tulis nama baru** is `<button type="button" class="cta-catat-bon">` with no `href` / `asp-action`.
- Do not change `_Layout.cshtml` or bottom nav.
- Do not commit unless the user asks.
- No test project; verify at `http://localhost:5032/Pelanggan` and with `dotnet build`.

## File map

- Modify: `Views/Pelanggan/Index.cshtml`
- Modify: `wwwroot/css/site.css`
- Unchanged: `Controllers/PelangganController.cs`, `Views/Shared/_Layout.cshtml`

---

### Task 1: Pelanggan markup

**Files:**
- Modify: `Views/Pelanggan/Index.cshtml`

**Interfaces:**
- Consumes: layout `ViewData["Title"]`, `.page-rule`, `.cta-catat-bon`
- Produces: static Pelanggan page body

- [ ] **Step 1:** Replace the placeholder view with the export structure (rule, visual search, count, ten cards, CTA). Full markup is in the view file; names/phones/addresses match the spec table exactly.

- [ ] **Step 2:** Confirm no `<input>`, `<form>`, `href`, or `asp-action` on cards or the CTA.

---

### Task 2: Pelanggan CSS

**Files:**
- Modify: `wwwroot/css/site.css`

**Interfaces:**
- Consumes: `--muted`, `--text`, `--border`, `--surface`
- Produces: `.pelanggan-page`, `.pelanggan-search`, `.pelanggan-search-icon`, `.pelanggan-search-text`, `.pelanggan-list-title`, `.pelanggan-cards`, `.pelanggan-card`, `.pelanggan-card-name`, `.pelanggan-card-meta`

- [ ] **Step 1:** Add those classes after `.cta-catat-bon`. Do not restyle dashboard, nav, or unused `.search-input` / `.content-card`.

- [ ] **Step 2:** Run `dotnet build`. Expected: success.

- [ ] **Step 3:** Open `/Pelanggan` and click through: tabs, CTA does not navigate, cards are not links, list scrolls above the nav.

Do not commit.
