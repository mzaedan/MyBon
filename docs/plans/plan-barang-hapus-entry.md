# Barang delete entry plan

**Goal:** Give `/Barang/Details/{id}` the same two-action row the Pelanggan sheet has, so the item sheet carries **Ubah barang** and **Hapus barang**.

**Spec:** `docs/superpowers/specs/2026-10-18-barang-detail-design.md` (supersedes its one-button constraint)

**Architecture:** View-only change. The row reuses `.pelanggan-form-actions`, `.pelanggan-btn-batal` (Ubah barang) and `.pelanggan-btn` + `.pelanggan-btn-hapus` (Hapus barang), so the row keeps the Pelanggan sheet's shape while the destructive action reads red instead of accent blue. No controller action, no service method, no model, no migration.

## File map

- Modify: `Views/Barang/Details.cshtml` (second action)
- Modify: `wwwroot/css/site.css` (`.pelanggan-btn-hapus`)
- Modify: `docs/superpowers/specs/2026-10-18-barang-detail-design.md` (update note)

---

## Slice 1: the entry point

- [ ] **Step 1:** In `Views/Barang/Details.cshtml`, keep `Ubah barang` on `Edit` but move it to `.pelanggan-btn-batal`, so the write action reads black like **Ubah data** on the Pelanggan sheet.
- [ ] **Step 2:** Add `Hapus barang` on `Delete` with `asp-route-id="@Model.Id"`, wearing `.pelanggan-btn pelanggan-btn-hapus`, and mark it with a `TODO:` Razor comment.
- [ ] **Step 3:** Add `.pelanggan-btn-hapus` to `site.css` beside `.pelanggan-btn-simpan`: background `#B00020` (the red the field rules already use), white text, plus `a.pelanggan-btn-hapus { color: #ffffff; }` so the global `a` accent does not win. The class lives in the shared button block, not the `.barang-*` block, because the Pelanggan sheet now wears it too.
- [ ] **Step 4:** `dotnet build`, then check `/Barang/Details/{id}` renders both buttons at 48px in one row, the second one red.

---

## Deliberately not done

- `BarangController.Delete` / `DeleteConfirmed`, `IBarangService.DeleteAsync`, a `Views/Barang/Delete.cshtml` confirmation screen, and `TempData["BarangStatus"]` on delete.
- The target action does not exist, so `/Barang/Delete/{id}` is a 404 until the delete flow is sliced. The button is a look, not a write, for now.

## Verification

- [ ] `/Barang/Details/{id}` shows **Ubah barang** (black) and **Hapus barang** (red `#B00020`), equal width, one row.
- [ ] **Ubah barang** still opens `/Barang/Edit/{id}` with the item's values.
- [ ] `href` of **Hapus barang** is `/Barang/Delete/{id}`, and its label stays white (not the global anchor accent).

> Update: the delete flow is now built. `BarangController.Delete` / `DeleteConfirmed`, `IBarangService.DeleteAsync`, `Views/Barang/Delete.cshtml`, and `TempData["BarangStatus"]` on delete exist, so the entry point reaches a real confirmation screen. See `docs/plans/plan-barang-hapus.md`.