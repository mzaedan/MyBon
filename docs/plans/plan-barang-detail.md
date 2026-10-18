# Barang detail plan

**Goal:** Give `/Barang` a read step that mirrors the Pelanggan flow, so a card opens `/Barang/Details/{id}` and the sheet carries **Ubah barang** into the existing edit form.

**Spec:** `docs/superpowers/specs/2026-10-18-barang-detail-design.md`

**Architecture:** Details is a third GET action on `BarangController`, reusing `IBarangService.GetByIdAsync`. No new model, service method, query, or migration. The view reuses the `.pelanggan-form*` classes the Barang forms already use, plus one price class in `site.css`.

## File map

- Modify: `Controllers/BarangController.cs` (add `Details`)
- Create: `Views/Barang/Details.cshtml`
- Modify: `Views/Barang/Index.cshtml` (card target)
- Modify: `wwwroot/css/site.css` (`.barang-detail-price`)
- Modify: `docs/superpowers/specs/2026-10-18-barang-edit-design.md`, `2026-10-18-barang-empty-state-design.md`, `2026-10-18-barang-create-persist-design.md` (update notes)

---

## Slice 1: the sheet

- [ ] **Step 1:** Add `Details(int id)` to `BarangController`: `GetByIdAsync`, then `ToActionResult(result, barang => ...)` setting `ViewData["Title"]` to the item name and `ViewData["BackHref"]` to `Index`, and returning `View(barang)`.
- [ ] **Step 2:** Add `Views/Barang/Details.cshtml`: `@model MyBon.Domain.Entities.Barang`, the same `FormatRupiah` local function the list uses, three `.pelanggan-field` blocks (Nama barang, Satuan with the `—` glyph, Harga with `.barang-detail-price`), each closed by `.page-rule`, then one action in `.pelanggan-form-actions` pointing at `Edit`.
- [ ] **Step 3:** Add `.barang-detail-price` to `site.css` beside the other `.barang-*` rules.
- [ ] **Step 4:** Point `a.barang-card` in `Views/Barang/Index.cshtml` at `Details`.
- [ ] **Step 5:** `dotnet build`, then check `/Barang` -> card -> `/Barang/Details/{id}` -> **Ubah barang** -> `/Barang/Edit/{id}`.

## Slice 2: honest docs

- [ ] **Step 1:** Append an update note to the three earlier Barang specs that still list an item detail page as out of scope, pointing at the new spec.

## Verification

- [ ] Card opens the sheet; header title is the item name; back and tab behave.
- [ ] Price reads `Rp 78.000` on both the card and the sheet; empty unit reads `—`.
- [ ] **Ubah barang** opens the edit form with the item's values.
- [ ] `/Barang/Details/99999` is a 404; an AJAX GET returns JSON.