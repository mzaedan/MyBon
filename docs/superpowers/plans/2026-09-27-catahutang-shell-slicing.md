# CatatHutang Shell Slicing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the generic MyBon mobile shell with a static CatatHutang slice: shop header, dashboard content, and four named-controller tabs.

**Architecture:** Shared `_Layout.cshtml` owns header and bottom nav. Four controllers (`Dashboard`, `Pelanggan`, `Barang`, `Bon`) each expose `Index`. Default MVC route lands on `Dashboard/Index`. `HomeController` keeps only Privacy and Error. Visual tokens live in `wwwroot/css/site.css`. Dashboard numbers are hardcoded in the Razor view.

**Tech Stack:** ASP.NET Core MVC, Razor, existing Bootstrap + Bootstrap Icons, Google Fonts Figtree/Fraunces, `site.css`.

## Global Constraints

- No Tailwind CDN.
- No models, services, or database for dashboard data.
- CTA **Catat bon baru** is `<button type="button">` with no `href` / `asp-action`.
- Do not render the mockup iPhone status bar.
- Privacy and Error stay on `HomeController`; they are not nav tabs.
- Do not commit unless the user asks.
- No test project exists; verify in the running app (`http://localhost:5032`) and with `dotnet build`.

## File map

- Create: `Controllers/DashboardController.cs`, `Controllers/PelangganController.cs`, `Controllers/BarangController.cs`, `Controllers/BonController.cs`
- Create: `Views/Dashboard/Index.cshtml`, `Views/Pelanggan/Index.cshtml`, `Views/Barang/Index.cshtml`, `Views/Bon/Index.cshtml`
- Modify: `Program.cs` (default controller `Dashboard`)
- Modify: `Controllers/HomeController.cs` (remove `Index`)
- Modify: `Views/Shared/_Layout.cshtml` (fonts, header, pill nav)
- Modify: `wwwroot/css/site.css` (tokens, header, nav, dashboard classes)
- Delete: `Views/Home/Index.cshtml`

---

### Task 1: Controllers and default route

**Files:**
- Create: `Controllers/DashboardController.cs`
- Create: `Controllers/PelangganController.cs`
- Create: `Controllers/BarangController.cs`
- Create: `Controllers/BonController.cs`
- Modify: `Controllers/HomeController.cs`
- Modify: `Program.cs`

**Interfaces:**
- Consumes: existing MVC routing
- Produces: `DashboardController.Index`, `PelangganController.Index`, `BarangController.Index`, `BonController.Index` each `IActionResult` returning `View()`; default URL `/` → Dashboard

- [ ] **Step 1:** Add the four controllers.

```csharp
namespace MyBon.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
```

Same pattern for `PelangganController`, `BarangController`, `BonController`.

- [ ] **Step 2:** Remove `Index` from `HomeController`; keep `Privacy` and `Error`.

- [ ] **Step 3:** In `Program.cs` set `pattern: "{controller=Dashboard}/{action=Index}/{id?}"`.

- [ ] **Step 4:** Run `dotnet build`. Expected: success. Do not commit.

---

### Task 2: Layout chrome

**Files:**
- Modify: `Views/Shared/_Layout.cshtml`

**Interfaces:**
- Consumes: `ViewData["Title"]`, current controller name from `ViewContext.RouteData`
- Produces: shop header; four `asp-controller` links with `IsActive(string controller)`

- [ ] **Step 1:** Add Figtree and Fraunces Google Fonts in `<head>`.

- [ ] **Step 2:** Replace header with brand mark `C`, shop name `Toko Kelontong Berkah`, and `@ViewData["Title"]`. No bell button.

- [ ] **Step 3:** Replace bottom nav with pill of Dashboard / Pelanggan / Barang / Catat Bon using `bi-house`/`bi-house-fill`, `bi-people`/`bi-people-fill`, `bi-box-seam`/`bi-box-seam-fill`, `bi-journal-text`. Active class when controller matches. Filled icon only when active.

---

### Task 3: CSS tokens and dashboard classes

**Files:**
- Modify: `wwwroot/css/site.css`

- [ ] **Step 1:** Set `--primary`/`--text` `#000000`, `--muted` `#5C5C5C`, `--accent` `#1877F2`, `--border` `#E5E5E5`, `--bg`/`--surface` `#FFFFFF`, `--max-w` `390px`, `--font` Figtree stack, `--font-display` Fraunces stack.

- [ ] **Step 2:** Restyle `.app-header` (no heavy bottom border; 20px horizontal padding), brand mark 28px black / 6px radius, shop 12px muted, title 22px.

- [ ] **Step 3:** Restyle `.bottom-nav` as overlay with 16px inset; inner `.bottom-nav-bar` white, 16px radius, 1px `#E5E5E5` outline; items 10px labels; `.active` color `#1877F2`.

- [ ] **Step 4:** Add dashboard classes: `.page-rule`, `.hutang-label`/`.hutang-value`/`.hutang-hint`, `.sold-row`, `.list-title`, `.debt-row`/`.debt-name`/`.debt-amount`, `.cta-catat-bon` (48px, `#1877F2`, 8px radius, white 16px semibold). Leave unused generic helpers unless they override header/main/nav.

---

### Task 4: Views

**Files:**
- Create: `Views/Dashboard/Index.cshtml`
- Create: `Views/Pelanggan/Index.cshtml`, `Views/Barang/Index.cshtml`, `Views/Bon/Index.cshtml`
- Delete: `Views/Home/Index.cshtml`

- [ ] **Step 1:** Dashboard view: `ViewData["Title"] = "Dashboard"` then rule, hutang block (`Rp 325.500`), sold row (`47`), **Belum lunas**, three debt rows, `<button type="button" class="cta-catat-bon">Catat bon baru</button>`. No header duplicate.

- [ ] **Step 2:** Placeholders set Title Pelanggan / Barang / Catat Bon and one sentence each.

- [ ] **Step 3:** Delete `Views/Home/Index.cshtml`.

- [ ] **Step 4:** `dotnet build`. Expected: success.

---

### Task 5: Browser verification

**Files:** none (running `dotnet watch` on http://localhost:5032)

- [ ] **Step 1:** `/` shows shop header, static dashboard, no iOS status bar, CTA does not navigate.
- [ ] **Step 2:** Each tab changes title and active color.
- [ ] **Step 3:** Desktop: 390px centered shell.
