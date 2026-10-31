# Bon hapus saat lunas plan

**Goal:** On `/Bon/Details/{id}`, a page that is already **Lunas** stops offering **Catat pembayaran** and **Tambah barang** and offers one destructive **Hapus bon** instead, through a confirmation screen that mirrors the Barang and Pelanggan delete flows.

**Depends on:** `Views/Bon/Details.cshtml`, `IBonService.GetDetailAsync` and its `Lunas` flag (`docs/plans/plan-bon-dinamis.md`), `bon_item` / `bon_payment` `OnDelete(Cascade)` in `Data/AppDbContext.cs`, the shared red `.pelanggan-btn-hapus` (`docs/plans/plan-tombol-hapus-merah.md`), and the comment on the Details actions (`docs/superpowers/specs/2026-10-03-detail-catat-bon-slicing-design.md`).

**Why:** A settled page has nothing left to record. The two write entry points (payment, line) stay for a page that still owes money, and the only write a settled page needs is removing itself. `docs/plans/plan-bon-dinamis.md` listed "edit and delete a bon" as a follow-up with no design; this plan answers the delete half for the settled case only.

## File map

- Modify: `Domain/Interfaces/IBonService.cs` (`DeleteAsync`)
- Modify: `Domain/Services/BonService.cs` (`DeleteAsync`)
- Modify: `Controllers/BonController.cs` (`Delete` GET, `DeleteConfirmed` POST)
- Modify: `Views/Bon/Details.cshtml` (settled pages swap the action row)
- Create: `Views/Bon/Delete.cshtml`
- Create: `wwwroot/custom/features/bon/delete.js`
- Modify: `wwwroot/css/site.css` (`.bon-action--hapus`)
- Modify: the plan and spec update notes listed above

---

## Slice 1: the write

- [ ] **Step 1:** Add `Task<ErrorOr> DeleteAsync(int id)` to `IBonService`.
- [ ] **Step 2:** Implement it in `BonService` the way `BarangService.DeleteAsync` works: read the bon's number plus its `bon_item` and `bon_payment` sums, return `NotFound` when the id is missing, refuse a page that still owes money with `Validation`, remove the row, save, and return `Unexpected` when the database rejects the delete.
- [ ] **Step 3:** Rely on the configured cascade: deleting the `bon` row removes its `bon_item` and `bon_payment` rows, so the service never loads them.

## Slice 2: the controller and the confirmation screen

- [ ] **Step 1:** Add `Delete(int id)` GET (loads through `GetDetailAsync` for the confirmation view) and `DeleteConfirmed(int id)` POST with `[ValidateAntiForgeryToken]` and `[ActionName("Delete")]`. AJAX goes through `ToActionResult`; a page request that succeeds sets `TempData["BonStatus"]` and redirects to `Index`; a refusal sets `TempData["BonError"]` and returns to `Details`.
- [ ] **Step 2:** Add `Views/Bon/Delete.cshtml` modelled on `Views/Barang/Delete.cshtml`: **Hapus bon** title, back to the page, trash Lottie, the copy naming the bon and its customer, then **Batal** (black, back to Details) and **Hapus bon** (red submit).
- [ ] **Step 3:** Add `wwwroot/custom/features/bon/delete.js` playing `Trash.json` in `#lottie-trash`, the same bootstrap the Pelanggan and Barang screens use.

## Slice 3: the settled action row

- [ ] **Step 1:** In `Views/Bon/Details.cshtml`, keep the two buttons for a page that still owes money and paint one **Hapus bon** link (`.bon-action .bon-action--hapus`, `bi-trash`) when `Model.Lunas`. A single link stretches to the full action row, so the settled page reads one full-width destructive action.
- [ ] **Step 2:** Add `.bon-action--hapus` beside `.bon-action--barang` with the shared `#B00020` red and a white label, plus the `a.bon-action--hapus` colour rule the global `a` accent needs.
- [ ] **Step 3:** Skip both bottom sheets too (`@if (!Model.Lunas)` around `#bonPaySheet` and `#bonAddSheet`), so a settled page carries no write form at all. `details.js` already returns `null` from `createSheet` and from `BonCalendar.create` when the elements they bind are missing, so nothing else changes.

---

## Deliberately not done

- No delete entry point on a page that still owes money: the service refuses it, so a crafted POST cannot erase a debt.
- No delete action on the list card or on Create; the settled Details page stays the only surface.
- No soft delete, undo, typed confirmation, or per-row copy.
- No new CSS block: the Pelanggan delete screen and the shared red button already cover the confirmation view.
- No edit of a bon; that follow-up stays open.

## Verification

- [ ] `/Bon/Details/{id}` on a page that still owes money: **Catat pembayaran** and **Tambah barang**, no **Hapus bon**.
- [ ] Record the last payment, and the repainted page shows one red **Hapus bon** instead of the two write buttons, and carries neither `#bonPaySheet` nor `#bonAddSheet`.
- [ ] **Hapus bon** opens `/Bon/Delete/{id}` with the title and back chevron; **Batal** returns to the page and the bon still exists.
- [ ] Submitting deletes the bon, lands on `/Bon` with `TempData["BonStatus"]`, and the card is gone; its `bon_item` and `bon_payment` rows are gone too.
- [ ] `/Bon/Delete/99999` returns 404; a POST for a page that still owes money answers on Details with **Hanya bon yang sudah lunas yang bisa dihapus**.
- [ ] `dotnet build` succeeds.
