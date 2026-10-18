# CatatHutang Barang create design

Date: 2026-10-18  
App: ASP.NET Core MVC MyBon  
Depends on: Barang list and form slice (`Views/Barang/Create.cshtml`), `IBarangService`, the Pelanggan create flow (`PelangganController.Create`, `PelangganService.CreateAsync`, `CreatePelangganRequest`), `BaseController`

Design read: the write-item screen for shopkeepers, CatatHutang tokens, ENERGY 1 / RHYTHM 1 / MOTION 1 (calm form, one action, an error beside the field that caused it).

Supersedes the form part of `2026-09-27-barang-list-form-slicing-design.md`: **Simpan barang** now writes to the `barang` table instead of re-rendering an empty form.

## Goal

Make **Simpan barang** save the item, so a shopkeeper who writes a name, a unit, and a price finds it on `/Barang` afterwards. The screen already has its layout; this adds persistence, validation, and the confirmation line.

## Constraints (agreed)

- Mirror Pelanggan Create: request model, service method, POST action, form re-rendered on failure. No second field system, no new form classes.
- Harga stays text in the request and the service parses it. Form binding parses a decimal with the host culture, so "78.000" would silently become 78 on an en-US host and 78000 on an id-ID one.
- Indonesian price reading: dots group thousands, a comma marks decimals. `78.000` is 78000, `1.234,5` is 1234.5, and `Rp 78.000` and `78 000` also parse.
- Letters and signs in Harga are rejected with a message instead of being stripped, so a mistyped field never saves a different amount than the one on screen.
- Amounts round to the column scale so the saved row and the JSON response report the same number.
- Errors render under their field through the existing `.pelanggan-field-error` markup, which is already styled and already turns the field rule red.
- Out of scope: live search on `/Barang`, item edit and delete, item detail, duplicate-name rules.

## Architecture

```
Models/Barang/Requests/CreateBarangRequest.cs   form input, data annotations
Domain/Interfaces/IBarangService.cs             CreateAsync
Domain/Services/BarangService.cs                trim, parse harga, save, save-failure handling
Controllers/BarangController.cs                 Create() GET, Create(CreateBarangRequest) POST
Views/Barang/Create.cshtml                      POST form, asp-for fields, validation spans
Views/Barang/Index.cshtml                       TempData status line
wwwroot/css/site.css                            .barang-notice
Views/_ViewImports.cshtml                       MyBon.Models.Barang.Requests
```

`CreateAsync` returns `ErrorOr<Barang>`. The controller sends success through `ToActionResult` to `Index` with `TempData["BarangStatus"]`, and failure back into the form through `AddErrorsToModelState`.

Field rules:

| Field | Rule |
|---|---|
| Nama | required, 200 max; annotation plus a whitespace check in the service |
| Satuan | optional, 30 max |
| Harga | required, 20 characters max, must parse and fit the column |

## Behaviour

1. `GET /Barang/Create` renders the empty form, as before.
2. A valid POST saves the row, then redirects to `/Barang` with **Barang tersimpan di buku** above the list.
3. A failed POST re-renders the form with the typed values kept and the message under the field: **Nama barang wajib diisi**, **Harga wajib diisi**, **Harga harus angka, contoh 78.000**, **Harga terlalu besar**.
4. AJAX callers receive the `ErrorOr` payload as JSON instead of a page, per `BaseController`.
5. The form carries the anti-forgery token; a POST without one is a 400.

## Styling

`.barang-notice` follows `.pelanggan-notice` (15px, muted, line-height 1.4) and sits in the Barang block of `site.css` with the other screen-level Barang rules. The form reuses the `.pelanggan-form*` classes unchanged.

## Data and errors

- `CreatedAt` is `DateTime.UtcNow`, because the `timestamp with time zone` column needs a UTC kind.
- A save failure that reaches the database (constraint, connection) detaches the new entity, logs it, and returns **Gagal menyimpan barang. Coba simpan lagi.** as `ErrorType.Unexpected`. Validation never throws.
- An empty notebook and a rejected price are different states: one is a successful empty list, the other is the form with a message beside the field.

## Reasons (R-31)

- **Why the price is parsed by hand**: the list shows `Rp 78.000`, so shopkeepers type dots. Culture-dependent binding would read that as 78 on one host and 78000 on another, which is a wrong amount in a debt notebook.
- **Why Harga stays a string in the request**: it is typed text, not yet a number, the same way `NomorHp` is typed text rather than an integer.
- **Why the notice reuses the Pelanggan metric**: it is the same confirmation in the same shell; a second look for the same sentence would split the design for no gain.
- **Why Batal and the back chevron still point at `/Barang`**: leaving the screen did not change.

## Verification

Run with `dotnet run` and the Postgres container up:

1. `/Barang` empty, then **Tulis barang baru** opens the form.
2. Name **Minyak Goreng 2 L**, satuan **botol**, harga **34.000** redirects to `/Barang` with the notice **Barang tersimpan di buku**, a card reading `Rp 34.000`, and a count line that follows.
3. Harga **1.234,5** stores 1234.50 and **Rp 28.000** stores 28000.00. Harga **dua ribu** re-renders with **Harga harus angka, contoh 78.000**; an empty form re-renders with both required messages; **9999999999999** re-renders with **Harga terlalu besar** and stores nothing.
4. A POST without `__RequestVerificationToken` is a 400.
5. `select nama, satuan, harga from barang` matches what was typed. Keyboard: Tab reaches every field, the focus ring is visible, and the error text is readable.

## Non-goals

Item edit and delete, an item detail page, live search on `/Barang`, barcode or photo fields, duplicate detection, a unit picker, and currency formatting inside the input.
