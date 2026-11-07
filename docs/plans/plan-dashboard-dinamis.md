# Dashboard dinamis plan

**Goal:** Make `/` read the notebook instead of painting hardcoded figures. Slice 1 replaced the big
**Total hutang semua customer** number, which sat at `Rp 325.500` whatever the notebook said, with the
sum of every bon that still owes money. Slice 2 did the same for **Jumlah produk terjual**, which sat
at `47`. Slice 3 replaced the three hardcoded names under **Belum lunas** with the three customers who
owe the most. Slice 4 turned the dead **Catat bon baru** button into the anchor that opens the form.

**Status:** Slices 1-4 implemented and verified against Postgres on 2026-11-07. `/` paints
**Rp 71.500** from `BN-001`, `BN-002`, and `BN-006`, **18** products sold from the 13 item lines, and
**Windah / Amba** as the customers who still owe money; every figure follows the notebook as soon as a
line, a payment, or a bon is written. **Catat bon baru** links to `/Bon/Create`, the same anchor the
Bon list paints.

## Architecture

Totals stay derived, exactly like the Bon list, Details, and the Create form's customer sheet:

```
total_hutang         = SUM(total_belanja - total_bayar) over every bon where total_belanja > total_bayar
total_belanja        = SUM(bon_item.harga_satuan * bon_item.jumlah)
total_bayar          = SUM(bon_payment.jumlah)
total_produk_terjual = SUM(bon_item.jumlah) over every line, paid page or not
belum_lunas          = the 3 customers with the biggest SUM of their clamped bon debts, name on ties
```

A settled page adds nothing to the debt, and a page whose payments somehow passed its total is skipped
instead of lowering another page's debt. A sold unit stays sold whether or not its page is paid, so
`bon_payment` never enters the product total. The sums run in the database — the debt ranking is one
`GROUP BY` query — so the page never loads every line to add them in memory.

`DashboardService` follows `BonService`: it injects `AppDbContext` directly and returns `ErrorOr<T>`.
`DashboardController` becomes a `BaseController` so the action goes through `ToActionResult()` and the
page re-renders with the service's own notice on failure.

## File map

- Create: `Models/Dashboard/DTOs/DashboardSummaryDto.cs` (`TotalHutang`, `TotalProdukTerjual`,
  `BelumLunas`)
- Create: `Models/Dashboard/DTOs/DashboardDebtItemDto.cs` (`PelangganId`, `Nama`, `SisaHutang`)
- Create: `Domain/Interfaces/IDashboardService.cs`, `Domain/Services/DashboardService.cs`
- Modify: `Program.cs` (`AddScoped<IDashboardService, DashboardService>()`)
- Modify: `Controllers/DashboardController.cs` (`BaseController`, async `Index`)
- Modify: `Views/Dashboard/Index.cshtml` (`@model`, `FormatRupiah`, the three real figures, list loop and
  empty state)
- Modify: `wwwroot/css/site.css` (`.debt-empty`)
- Create: `docs/plans/plan-dashboard-dinamis.md` (this plan)

## Slice 1: total hutang

- [x] **Step 1:** `DashboardSummaryDto.TotalHutang` carries what every customer still owes. The DTO is
      the view's model, like `BonDetailDto` on Details.
- [x] **Step 2:** `GetSummaryAsync` projects each bon to its `TotalBelanja` and `TotalBayar`, keeps the
      pages where `TotalBelanja > TotalBayar`, and sums the difference in one `SUM`, so EF sends the
      arithmetic to Postgres.
- [x] **Step 3:** `/` renders `FormatRupiah(Model.TotalHutang)` with the same `N0` / `id-ID` reading the
      Bon cards use, so the big number matches the amounts on the list.
- [x] **Step 4:** `dotnet build` succeeds: 0 warnings, 0 errors.

## Slice 2: jumlah produk terjual

- [x] **Step 1:** `DashboardSummaryDto.TotalProdukTerjual` carries how many units the notebook sold, so
      the view has both figures from one service call.
- [x] **Step 2:** `GetSummaryAsync` reads `SUM(bon_item.jumlah)` over every line, settled pages included,
      because a unit handed over is sold even while its money is still owed. The sum runs in Postgres.
- [x] **Step 3:** `/` renders the count with the same `N0` / `id-ID` grouping as the money, so `1018`
      reads **1.018** and `18` stays **18**.

## Slice 3: belum lunas

- [x] **Step 1:** `DashboardDebtItemDto` carries one row of the list, and
      `DashboardSummaryDto.BelumLunas` carries the ranked rows, so the view paints what the service
      ranked instead of three fixed names.
- [x] **Step 2:** `GetSummaryAsync` clamps every bon to `lines - payments`, keeps the pages that still
      owe money, groups by customer, and takes the top three (`TopDebtCustomerCount`). Ranking happens
      in Postgres: one `GROUP BY` query with the same correlated sums as the debt total.
- [x] **Step 3:** The order is `SisaHutang` descending with `Nama` as the tie-break, so two customers who
      owe the same amount keep a stable order across requests.
- [x] **Step 4:** `/` paints the rows with `@FormatRupiah(debt.SisaHutang)`; when nobody owes anything it
      paints **Belum ada hutang. Semua bon lunas.** (`.debt-empty`) instead of an empty list.

## Slice 4: catat bon baru

- [x] **Step 1:** The `<button type="button">` became `<a class="cta-catat-bon" asp-controller="Bon"
      asp-action="Create">`, the same anchor and label the Bon list paints, so both roads reach the form.
- [x] **Step 2:** No CSS change was needed: `.cta-catat-bon` already carried `text-decoration: none` and
      the `a.cta-catat-bon` rule that keeps the label white.

## Verification (Postgres, 2026-11-07)

- [x] `/` answered `200` and painted `hutang-value">Rp 71.500`, which is `BN-001` 4.500 + `BN-002`
      10.000 + `BN-006` 57.000, the same figures `BonService` reports per page.
- [x] A temporary `bon_payment` of **500** against `BN-001` moved the page to **Rp 71.000**.
- [x] Paying the rest (**4.000**) settled `BN-001`, and the page read **Rp 67.000**, so a page that owes
      nothing leaves the total instead of adding zero.
- [x] Both test payments were deleted again: the page reads **Rp 71.500** and `bon_payment` holds only
      its two real rows (2.500 on `BN-002`, 10.000 on `BN-006`).
- [x] `/` painted `sold-value">18`, the 13 item lines adding up to 18 units, so the count starts from the
      real notebook rather than `47`.
- [x] A temporary `bon_item` of **3** units raised the page to **21** products and **Rp 71.800**, and
      deleting it brought both back to **18** and **Rp 71.500**.
- [x] A temporary line of **1000** units raised the count to **1.018** (grouped) and the debt to
      **Rp 72.500**; deleting it restored **18** and **Rp 71.500**.
- [x] Every test row was deleted again: `bon_item` holds its 13 rows / 18 units and `bon_payment` its two
      real rows.
- [x] `/` painted the two real debtors in order: **Windah / Rp 57.000** then **Amba / Rp 14.500**, which
      is what `SUM(bon_item x jumlah) - SUM(bon_payment)` says per customer.
- [x] Two temporary bons of **1.000** each for **Reza Auditore** and **Ladesh** created four debtors; the
      page painted only three rows — **Windah**, **Amba**, **Ladesh** — so the cap holds, and the 1.000
      tie fell on the name. The temp bons were deleted again (cascade removed their lines) and the page
      dropped back to **Windah**, **Amba**.
- [x] Paying every page off (`BN-001` 4.500, `BN-002` 10.000, `BN-006` 57.000) made the page read
      **Rp 0** and **Belum ada hutang. Semua bon lunas.**; the three test payments were deleted again
      and the page returned to **Rp 71.500** with its two rows.
- [x] Final counts after the cleanup: 3 bons, 13 item lines, 2 payments — the notebook is exactly as it
      was before the tests.
- [x] `/` painted `<a class="cta-catat-bon" href="/Bon/Create">Catat bon baru</a>`, so the summary's one
      write entry point reaches the form, and `GET /Bon/Create` answered `200` with title
      **Bon baru - MyBon**.

## Follow-ups (not this plan)

- **Lihat semua** for the **Belum lunas** list: the page caps at three rows, and the notebook has no
  screen yet for the full ranking (candidates: `/Pelanggan` ordered by debt, or its own list page).

## Non-goals

Caching or a periodic refresh, AJAX repaint of the number, a chart, filters by date or customer, a new
JS library.
