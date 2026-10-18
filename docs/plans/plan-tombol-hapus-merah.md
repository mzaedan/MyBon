# Tombol hapus merah plan

**Goal:** One destructive red for every **Hapus** control, so `/Pelanggan/Details/{id}`, `/Pelanggan/Delete/{id}`, and `/Barang/Details/{id}` mark the delete action the same way.

**Spec:** `docs/superpowers/specs/2026-09-27-detail-pelanggan-slicing-design.md`, `2026-10-17-hapus-pelanggan-slicing-design.md`, and `2026-10-18-barang-detail-design.md` (all carry update notes)

**Architecture:** One shared class in the button block of `site.css`, not a per-page class. No controller, service, model, or markup-structure change.

## File map

- Modify: `wwwroot/css/site.css` (`.pelanggan-btn-hapus` beside `.pelanggan-btn-simpan`)
- Modify: `Views/Pelanggan/Details.cshtml` (Hapus dari buku)
- Modify: `Views/Pelanggan/Delete.cshtml` (Hapus data submit)
- Modify: `Views/Barang/Details.cshtml` (Hapus barang, replacing `.barang-btn-hapus`)
- Modify: the three specs above (update notes)

---

## Slice 1: the shared class

- [ ] **Step 1:** Add `.pelanggan-btn-hapus` and `a.pelanggan-btn-hapus` after `.pelanggan-btn-simpan`: `background: #B00020` (the red the field rules already use), `color: #ffffff`. The anchor rule is required because the global `a` colour is the accent blue; the bare class covers the `<button>` on the confirmation screen.
- [ ] **Step 2:** In `Views/Pelanggan/Details.cshtml`, move **Hapus dari buku** from `.pelanggan-btn-simpan` to `.pelanggan-btn-hapus`. **Ubah data** keeps `.pelanggan-btn-batal` (black).
- [ ] **Step 3:** In `Views/Pelanggan/Delete.cshtml`, move the **Hapus data** submit from `.pelanggan-btn-simpan` to `.pelanggan-btn-hapus`. **Batal** keeps `.pelanggan-btn-batal` (black).
- [ ] **Step 4:** In `Views/Barang/Details.cshtml`, move **Hapus barang** to `.pelanggan-btn-hapus` and delete the now-unused `.barang-btn-hapus` rules.
- [ ] **Step 5:** `dotnet build`, then check both detail sheets and the confirmation screen render one black and one red action.

---

## Deliberately not done

- No new palette token, no hover/transition, no `--danger` variable: the red stays a literal `#B00020`, the way the field rules already write it.
- Item delete is still unimplemented, so the Barang button remains an entry point whose target 404s.
- `pelanggan-empty-btn` and the Bon actions keep the accent blue: they are not delete entry points.

## Verification

- [ ] `/Pelanggan/Details/{id}`: **Ubah data** black, **Hapus dari buku** red with white label.
- [ ] `/Pelanggan/Delete/{id}`: **Batal** black, **Hapus data** red with white label; submitting still deletes and lands on the list.
- [ ] `/Barang/Details/{id}`: **Ubah barang** black, **Hapus barang** red with white label.
- [ ] All rows stay equal width at 48px; focus rings still visible.
- [ ] No `.barang-btn-hapus` left in `site.css`, views, or docs.