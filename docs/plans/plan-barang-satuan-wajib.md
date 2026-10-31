# Barang satuan wajib plan

**Goal:** Make the **Satuan** field on `/Barang/Create` required, so an item cannot be saved without a unit and the form says **Satuan wajib diisi** under the field, matching **Nama barang wajib diisi** and **Harga wajib diisi**.

**Spec:** `docs/superpowers/specs/2026-10-18-barang-create-persist-design.md`

**Architecture:** One rule change on the existing create request model. `CreateBarangRequest.Satuan` becomes a required (`[Required]`), non-nullable property, the same shape `Nama` and `Harga` already use. The controller already checks `ModelState.IsValid` and returns `InvalidInputResult`, and the view already renders `<span asp-validation-for="Satuan">` inside a form that loads `_ValidationScriptsPartial`, so the message renders client-side and server-side with no Razor, controller, or service change. A caller that bypasses the field (AJAX or direct POST) is still rejected by the same `ModelState` check, because the controller validates before the service runs.

This change is scoped to create. `/Barang/Edit` keeps an optional satuan for now, so the shared `BarangService.ReadFields` reader (used by both create and update) is left untouched.

## File map

- Modify: `Models/Barang/Requests/CreateBarangRequest.cs` (`Satuan`)
- Modify: `docs/superpowers/specs/2026-10-18-barang-create-persist-design.md` (field table, behaviour, update note)

---

## Slice 1: satuan is required

- [x] **Step 1:** On `CreateBarangRequest.Satuan`, add `[Required(ErrorMessage = "Satuan wajib diisi")]`, keep `[MaxLength(30, ...)]`, and change the property from `string? Satuan { get; set; }` to `string Satuan { get; set; } = null!;`.
- [x] **Step 2:** Leave `Views/Barang/Create.cshtml` as is: it already has `<span asp-validation-for="Satuan" class="pelanggan-field-error">` and the `_ValidationScriptsPartial` that wires jQuery unobtrusive validation.
- [x] **Step 3:** Leave `BarangController.Create` and `BarangService` as is: `ModelState.IsValid` already runs before the service, so both page and AJAX posts are refused.

## Behaviour

| Action | Before | After |
|---|---|---|
| Save with satuan filled | Saves | Saves |
| Save with satuan empty | Saves with a null satuan | Re-renders with **Satuan wajib diisi** under the field; nothing is written |
| Save with satuan spaces only | Saves with a null satuan | Re-renders with **Satuan wajib diisi** (`[Required]` rejects whitespace) |
| Save with satuan over 30 characters | Re-renders with **Satuan maksimal 30 karakter** | Unchanged |
| AJAX POST with an empty satuan | Writes a row | 400 with the errors grouped by field |

## Deliberately not done

- `/Barang/Edit`: the request keeps its optional satuan in this slice. Change `UpdateBarangRequest` the same way if edit should also require a unit.
- A unit picker or a suggested-units list: the field stays free text.
- A whitespace check inside `BarangService.ReadFields`: `[Required]` already rejects a blank or whitespace-only value before the service runs, and the reader is shared with edit.
- Tightening the `barang.satuan` column to `NOT NULL`: the schema stays nullable, because edit can still save rows without a unit.

## Verification

- [ ] `dotnet build`.
- [ ] `/Barang/Create`: submitting with an empty **Satuan** shows **Satuan wajib diisi** under the field and writes no row.
- [ ] `/Barang/Create`: a satuan of spaces only shows the same message.
- [ ] `/Barang/Create`: **Simpan barang** with name, satuan **botol**, and harga **34.000** still saves and lands on `/Barang`.
- [ ] `POST /Barang/Create` with `X-Requested-With: XMLHttpRequest` and an empty `Satuan` returns 400 with `Satuan` in the error payload.
- [ ] `/Barang/Edit/{id}` still saves with an empty satuan (unchanged).
