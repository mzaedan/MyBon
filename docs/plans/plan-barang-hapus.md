# Barang delete flow plan

**Goal:** Let **Hapus barang** on `/Barang/Details/{id}` actually delete the item, through a confirmation screen that mirrors the Pelanggan delete flow.

**Spec:** `docs/superpowers/specs/2026-10-18-barang-detail-design.md` and `docs/plans/plan-barang-hapus-entry.md` (both deferred the delete flow)

**Architecture:** The same GET/POST pair the Pelanggan controller uses. `Delete(int id)` loads the item for the confirmation view, `DeleteConfirmed` calls the new `IBarangService.DeleteAsync`, sets `TempData["BarangStatus"]`, and redirects to `/Barang`. The view reuses the `.pelanggan-delete*` and `.pelanggan-btn*` classes already in `site.css`, so no new CSS and no migration.

## File map

- Modify: `Domain/Interfaces/IBarangService.cs` (`DeleteAsync`)
- Modify: `Domain/Services/BarangService.cs` (`DeleteAsync`)
- Modify: `Controllers/BarangController.cs` (`Delete` GET, `DeleteConfirmed` POST)
- Create: `Views/Barang/Delete.cshtml`
- Create: `wwwroot/custom/features/barang/delete.js`
- Modify: `Views/Barang/Details.cshtml` (drop the TODO on the entry point)
- Modify: `docs/plans/plan-barang-hapus-entry.md` and `docs/superpowers/specs/2026-10-18-barang-detail-design.md` (update notes)

---

## Slice 1: the write

- [ ] **Step 1:** Add `Task<ErrorOr> DeleteAsync(int id)` to `IBarangService`.
- [ ] **Step 2:** Implement it in `BarangService` the way `PelangganService.DeleteAsync` works: load the row, return `NotFound` when it is missing, remove it, save, detach and return `Unexpected` when the database rejects the delete.
- [ ] **Step 3:** Add `Delete(int id)` GET to `BarangController` (load for the confirmation view) and `DeleteConfirmed(int id)` POST with `[ValidateAntiForgeryToken]` and `[ActionName("Delete")]`: AJAX and failed requests go through `ToActionResult`, a page request sets `TempData["BarangStatus"]` and redirects to `Index`.

---

## Slice 2: the confirmation screen

- [ ] **Step 1:** Add `Views/Barang/Delete.cshtml` modelled on `Views/Pelanggan/Delete.cshtml`: `Hapus barang` title, back to the item sheet, trash Lottie, the copy **Nama, satuan, dan harga hilang dari daftar.**, then **Batal** (black, back to Details) and **Hapus barang** (red submit) in `.pelanggan-form-actions .pelanggan-delete-actions`.
- [ ] **Step 2:** Add `wwwroot/custom/features/barang/delete.js` playing `Trash.json` in `#lottie-trash`, the same bootstrap the Pelanggan screen uses.
- [ ] **Step 3:** Remove the TODO comment on the **Hapus barang** link in `Views/Barang/Details.cshtml`; the target now exists.
- [ ] **Step 4:** `dotnet build`, then check the full loop on `/Barang`.

---

## Deliberately not done

- No soft delete, undo, typed confirmation, or per-item copy.
- No new CSS class: the Pelanggan delete block and the shared red button already cover this screen.
- No Bon line check: `bon_item.barang_id` is `ON DELETE SET NULL`, so a delete is not blocked; the line keeps its name, unit, and price snapshot.
- No delete action on the list card; the item sheet stays the only write surface.

## Verification

- [ ] `/Barang` → open an item → **Hapus barang** opens `/Barang/Delete/{id}` with the title and back chevron.
- [ ] Back and **Batal** return to `/Barang/Details/{id}`; the item still exists.
- [ ] **Hapus barang** deletes the item, lands on `/Barang` with `TempData["BarangStatus"]`, and the card is gone.
- [ ] `/Barang/Delete/99999` returns 404; a failed POST keeps the error handling of `ToActionResult`.
- [ ] Deleting the last item leaves the existing Barang empty state.
