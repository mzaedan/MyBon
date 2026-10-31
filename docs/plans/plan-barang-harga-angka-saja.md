# Barang harga angka saja plan

**Goal:** Keep letters and other symbols out of the **Harga** field on `/Barang/Create` and `/Barang/Edit`, so the price can only be typed as digits, dots, and a comma.

**Spec:** `docs/superpowers/specs/2026-10-18-barang-create-persist-design.md`, `docs/superpowers/specs/2026-10-18-barang-edit-design.md`

**Architecture:** One behaviour change inside the existing shared script `wwwroot/custom/features/barang/harga-input.js`. No Razor, controller, service, model, or CSS change. The server-side parse and the **Harga harus angka, contoh 78.000** message stay as a safety net for callers that bypass the field (AJAX, direct POST).

> Update: the script moved to `wwwroot/helpers/money-input.js` so Bon > Details could reuse it for
> **Jumlah bayar** without reaching into a feature folder. The behaviour below is unchanged; only the
> `<script src>` in `Views/Barang/Create.cshtml` and `Views/Barang/Edit.cshtml` points at the helper.
> See `docs/plans/plan-jumlah-bayar-angka-saja.md`.

## File map

- Modify: `wwwroot/helpers/money-input.js` (was `wwwroot/custom/features/barang/harga-input.js`)
- Modify: `docs/superpowers/specs/2026-10-18-barang-create-persist-design.md`, `2026-10-18-barang-edit-design.md` (update notes)

---

## Slice 1: the field rejects letters

- [ ] **Step 1:** In `harga-input.js`, add a `beforeinput` listener that calls `preventDefault()` for `insertText` carrying a character outside `[0-9.,]`. Typed letters and signs never reach the field.
- [ ] **Step 2:** Change `normalise` to strip every non-price character (`[^0-9.,]`) before formatting, instead of returning early. This covers paste, drop, autofill, and IME input, which do not fire `insertText`.
- [ ] **Step 3:** Keep the submit handler that strips dots so the posted `harga` is plain digits.

## Behaviour

| Action | Before | After |
|---|---|---|
| Type `a` | Field shows `a`; server rejects on submit | Nothing appears in the field |
| Type `-` | Field shows `-`; server rejects on submit | Nothing appears in the field |
| Paste `Rp 78.000` | Field keeps `Rp 78.000`; server rejects | Field shows `78.000` |
| Paste `78abc000` | Field keeps `78abc000`; server rejects | Field shows `78.000` |
| Type `34.000` | Field shows `34.000`; posts `34000` | Unchanged |

## Deliberately not done

- `type="number"`: it drops thousand separators, blocks the comma on some browsers, and reintroduces culture-dependent parsing.
- Removing the service parse or the error sentence: a direct POST still needs a rejection path.
- The Bon form's **Total Bayar** field: it has its own input, outside this request.

## Verification

- [ ] `node --check wwwroot/custom/features/barang/harga-input.js`.
- [ ] `dotnet build`.
- [ ] `/Barang/Create`: typing `dua ribu` leaves the field empty; typing `34.000` shows `34.000` and saves 34000.
- [ ] Pasting `Rp 28.000` leaves `28.000` in the field and saves 28000.
- [ ] `/Barang/Edit/{id}` behaves the same and still prefills the stored price.
- [ ] A direct POST of `harga=dua ribu` still returns **Harga harus angka, contoh 78.000**.
