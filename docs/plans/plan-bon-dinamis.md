# Bon dinamis plan

**Goal:** Make **Catat Bon** read and write the notebook. `/Bon` lists real bons, `/Bon/Create` saves a header plus its item lines, and `/Bon/Details/{id}` reads one bon back. Today all three views are hardcoded markup and `BonController` never touches the database.

**Status:** Slices 1-4 are implemented and verified against Postgres on 2026-10-25; Slice 5 was added on 2026-10-31. `/Bon` reads real bons through `BonService`, `/Bon/Create` loads both sheets from the notebook and saves a header with its lines and first payment, and `/Bon/Details/{id}` reads one bon through `BonDetailDto` and paints its own header, totals, payment history, and item lines. The item picker on Create asks the server for its rows instead of carrying the whole stock list, and a rejected POST repaints the lines it already had. The two bottom sheets on Details are still visual, so recording a payment and writing a line wait for the Follow-ups. Both decisions in [Open decisions](#open-decisions) are locked.

## Architecture

Three tables behind one service, following the shape `BarangService` already uses: `BonService` injects `AppDbContext` directly, returns `ErrorOr<T>` / `PagedResult<T>`, and `BonController` becomes a `BaseController` so every action goes through `ToActionResult()`.

Totals are never stored. `bon_item` freezes the price it was recorded at, so the sums stay true even after a master price changes:

```
total_belanja = SUM(bon_item.harga_satuan * bon_item.jumlah)
total_bayar   = SUM(bon_payment.jumlah)
sisa_hutang   = total_belanja - total_bayar
status        = sisa_hutang <= 0 ? "Lunas" : "Belum lunas"
```

## File map

- Created: `Domain/Entities/Bon.cs`, `BonItem.cs`, `BonPayment.cs`
- Modified: `Data/AppDbContext.cs` (`DbSet`s, `HasSequence("bon_nomor_seq")`, snake_case mapping)
- Created: `Migrations/20261025123835_CreateBonTables.cs` (via `dotnet ef migrations add CreateBonTables --output-dir Migrations`)
- Create: `Domain/Interfaces/IBonService.cs`, `Domain/Services/BonService.cs`
- Create: `Models/Bon/Requests/CreateBonRequest.cs`, `Models/Bon/Requests/CreateBonItemRequest.cs`
- Create: `Models/Bon/DTOs/BonListItemDto.cs`, `BonDetailDto.cs`, `BonDetailItemDto.cs`, `BonDetailPaymentDto.cs`
- Create: `Models/Bon/DTOs/BonFormDto.cs`, `BonCustomerOptionDto.cs`, `BonBarangOptionDto.cs`
- Create: `Models/Bon/ViewModels/CreateBonViewModel.cs`
- Modify: `Program.cs` (`AddScoped<IBonService, BonService>()`)
- Modify: `Controllers/BonController.cs` (`BaseController`, real `Index`, `Search`, `Create`, `Details`)
- Modify: `Views/Bon/Index.cshtml`, `Views/Bon/Create.cshtml`, `Views/Bon/Details.cshtml`
- Create: `Views/Bon/_Cards.cshtml` (list partial, mirrors `Views/Barang/_Cards.cshtml`)
- Create: `wwwroot/custom/features/bon/index.js` (load-more + search, mirrors `barang/index.js`)
- Modify: `wwwroot/css/site.css` (empty state, validation text)

---

## Slice 1: schema

Tables are singular and snake_case, matching `pelanggan` and `barang`.

### `bon` — one row per bon

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `integer` identity | no | PK, used in `/Bon/Details/{id}` |
| `nomor` | `varchar(20)` | no | `BN-001`, unique index, assigned by the DB default |
| `pelanggan_id` | `integer` | no | FK -> `pelanggan.id`, `OnDelete(Restrict)` |
| `tanggal` | `date` | no | `DateOnly`, the bon date; no time of day in this notebook |
| `catatan` | `text` | yes | Form field **Catatan** |
| `created_at` | `timestamptz` | no | `BaseEntity`, default `CURRENT_TIMESTAMP` |

### `bon_item` — item line

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `integer` identity | no | PK |
| `bon_id` | `integer` | no | FK -> `bon.id`, `OnDelete(Cascade)` |
| `barang_id` | `integer` | yes | FK -> `barang.id`, `OnDelete(SetNull)` so **Hapus barang** keeps working |
| `nama_barang` | `varchar(200)` | no | Snapshot at recording time |
| `satuan` | `varchar(30)` | yes | Snapshot at recording time |
| `harga_satuan` | `numeric(14,2)` | no | Snapshot at recording time, `CHECK (harga_satuan >= 0)` |
| `jumlah` | `integer` | no | Whole units only, default `1`, `CHECK (jumlah >= 1)` |
| `tanggal` | `date` | no | Default `CURRENT_DATE`; the date Details prints next to the line |
| `created_at` | `timestamptz` | no | `BaseEntity` audit field, never printed |

### `bon_payment` — payment history

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `integer` identity | no | PK |
| `bon_id` | `integer` | no | FK -> `bon.id`, `OnDelete(Cascade)` |
| `tanggal` | `date` | no | Payment date |
| `jumlah` | `numeric(14,2)` | no | `CHECK (jumlah > 0)` |
| `catatan` | `varchar(200)` | yes | Free text from the sheet; **Cicilan 1** labels are rendered, not stored |
| `created_at` | `timestamptz` | no | `BaseEntity` |

### Numbering

A dedicated sequence keeps `BN-001` sequential and safe when two tabs save at once, with no `MAX(nomor) + 1` race and no extra round trip: Postgres assigns the value and EF reads it back through `RETURNING`.

```csharp
modelBuilder.HasSequence<int>("bon_nomor_seq", sequence =>
{
    sequence.StartsAt(1);
    sequence.IncrementsBy(1);
});

entity.Property(e => e.Nomor)
      .HasColumnName("nomor")
      .HasMaxLength(20)
      .IsRequired()
      // BN-001 ... BN-999, then BN-1000: lpad only pads up to three digits.
      .HasDefaultValueSql("'BN-' || lpad(nextval('bon_nomor_seq')::text, 3, '0')")
      .ValueGeneratedOnAdd();

entity.HasIndex(e => e.Nomor).IsUnique();
```

A rolled-back insert burns a number, so the series can skip. That is accepted: a missing `BN-004` is safer than a duplicate.

### Indexes

| Index | Reason |
|---|---|
| unique `bon(nomor)` | no duplicates; also the search target |
| `bon(tanggal desc, id desc)` | list order |
| `bon(pelanggan_id, tanggal desc, id desc)` | a customer's bons, newest first; the leading column also serves the FK |
| `bon_item(bon_id)` | lines of one bon |
| `bon_item(barang_id)` | usage check before an item is deleted |
| `bon_payment(bon_id, tanggal)` | payment history of one bon, oldest first |

- [x] **Step 1:** Add the three entities, inheriting `BaseEntity`, FK plus nullable navigation on the same block (`PelangganId` + `Pelanggan?`), collections initialised with `[]`.
- [x] **Step 2:** Map them in `AppDbContext.OnModelCreating` with the column names, lengths, `HasPrecision(14, 2)` on money, the check constraints, and the sequence above.
- [x] **Step 3:** `dotnet ef migrations add CreateBonTables --output-dir Migrations` then `dotnet ef database update`. The generated SQL was read back and matches: sequence created, `nomor` default expression, `ON DELETE RESTRICT` / `SET NULL` / `CASCADE`, three check constraints, and both descending indexes. `dotnet ef migrations has-pending-model-changes` reports no drift.
- [x] **Step 4:** `PelangganService.DeleteAsync` maps a Postgres foreign key violation (`23503`) to `ErrorType.Conflict` with **Pelanggan masih punya bon**, ahead of the generic save failure.

### Schema verification (Postgres, rolled-back transaction)

- `bon.nomor` and `bon.tanggal` are filled by their defaults: `BN-001`, `CURRENT_DATE`.
- `bon_item.tanggal` is filled by `CURRENT_DATE`.
- `jumlah = 0`, `harga_satuan = -5`, and `bon_payment.jumlah = 0` are rejected by the three check constraints.
- Deleting a `barang` sets `bon_item.barang_id` to NULL and keeps `nama_barang`, `satuan`, and `harga_satuan`.
- Deleting a `pelanggan` who owns a bon is rejected by `FK_bon_pelanggan_pelanggan_id`.
- Deleting a `bon` cascades its `bon_item` and `bon_payment` rows.
- The test ran inside a transaction that was rolled back, and the sequences were reset afterwards, so the first real bon is `BN-001`.

---

## Slice 2: read

- [x] **Step 1:** `IBonService.SearchAsync(string? keyword, int page)` mirrors `BarangService`: `ILIKE` over `bon.nomor` and `pelanggan.nama`, `OrderByDescending(Tanggal).ThenByDescending(Id)`, `PageSize = 8`, returns `PagedResult<BonListItemDto>` carrying the numbers `/Bon` paints.
- [x] **Step 2:** `GetDetailAsync(int id)` serves the read (one call for Details): `AsNoTracking` projection of `Pelanggan.Nama`, `Payments` and `Items`, so a split query is not needed. `BonDetailDto` computes the totals, `SisaHutang` and the status label; a missing id is `ErrorType.NotFound`.
- [x] **Step 3:** `Index` asks page 1; `Search(q, page = 1)` answers an XHR with `_Cards`. `Details(int id)` now reads through `GetDetailAsync`; see Slice 4 Step 4.
- [x] **Step 4:** `/Bon/Details/99999` returns 404 through `ToActionResult`, and an AJAX call returns the `ErrorOr` JSON. Verified against Postgres on 2026-10-25: the page answered `404`, the XHR answered `200` with `{"data":null,"message":"Bon tidak ditemukan.","success":false,"errorType":1}`.

## Slice 3: write

- [x] **Step 1:** `CreateBonRequest`: `PelangganId` (required), `Tanggal` (required), `Catatan`, `TotalBayar` as text parsed like `CreateBarangRequest.Harga`, and `List<CreateBonItemRequest>` (`BarangId`, `Jumlah`).
- [x] **Step 2:** `CreateAsync` validates: customer exists, at least one line, every line has an existing barang, `jumlah >= 1`, `0 <= total bayar <= total belanja`. Prices are read from `barang`, never from the client, so a tampered page cannot set a price.
- [x] **Step 3:** Save header and lines in one transaction; a total bayar above zero also writes the first `bon_payment` row, so `bon_payment` stays the only source of paid amounts.
- [x] **Step 4:** `Create` GET loads the customer list and the item list for the customer sheet and the item picker through `IBonService.GetFormAsync`; `Create` POST uses `[ValidateAntiForgeryToken]`, and a failure reloads both sheets and re-renders the form through `AddErrorsToModelState`.
- [x] **Step 5:** On success `TempData["BonStatus"]` carries **Bon BN-0xx tersimpan** and the action redirects to `Index`.

### Create verification (Postgres)

- A POST with two lines and **Total Bayar** `20.000` answered `302` to `/Bon`, wrote `BN-001` (total `43.000`), two `bon_item` rows with the prices copied from `barang`, and one `bon_payment` row of `20.000`.
- `/Bon` then painted the card with **Rp 43.000** / **Rp 23.000**, and the customer sheet reported the same sisa for that customer.
- An empty field list, a total above the sum of the lines, a non-numeric total, and a barang id that does not exist each answered `200` with the message rendered on the form; the two sheets stayed filled.
- A POST without the antiforgery token answered `400`; an AJAX POST answered the `ErrorOr` JSON instead of a page.
- The test bons were deleted and `bon_nomor_seq` was restarted, so the first real bon is still `BN-001`.
- The UI renumbers `Items[i]` before submitting: the collection binder only walks indexes 0, 1, ... and stops at the first gap, so a removed middle line can never drop the line after it.

## Slice 4: views and JS

- [x] **Step 1:** `Index.cshtml` renders `_Cards` in a loop, deletes the four hardcoded cards, keeps `.bon-card*`, `.bon-search*` and `.cta-catat-bon`, and gains the empty state the Barang list has. The search row is a real `q` field, the title reports the total, and the stack carries `data-search-url` so `index.js` can page and search it.
- [x] **Step 2:** `Create.cshtml` turns `method="get"` into a POST form with an antiforgery token, fills the customer rows in `#bonCustomerPicker` from the model (name, phone, sisa), keeping `data-nama` on each `.bon-customer-item`, and replaces the hardcoded picker buttons with the items the page carries (`data-id`, `data-nama`, `data-satuan`, `data-harga`), so the hidden field submits the id. The customer sheet filters its rendered rows by name and the date field starts on today; the item picker's rows moved to Slice 5.
- [x] **Step 3:** `create.js` posts `Items[i].BarangId` / `Items[i].Jumlah`, renumbering the lines in order on every add, remove, and submit so model binding fills the list; the subtotal and total arithmetic read the raw price instead of the painted label.
- [x] **Step 4:** `Details(int id)` reads one bon through `IBonService.GetDetailAsync` and returns `BonDetailDto`: header from `Nomor` and `NamaPelanggan`, summary from the computed totals, history from `Payments` (oldest first, `Cicilan n` rendered from the row order), lines from `Items` with `harga_satuan x jumlah satuan`, and the DTO's `Status` beside the date. `ViewData["Title"]` is the number, `BackHref` is the list, and a missing id is a 404. The payment sheet's hint reads the real sisa (or **Bon ini sudah lunas.**) and its date starts on today, but **Simpan pembayaran** and a picked item still write nothing: those stay Follow-ups. Empty history and an empty line list get their own muted note (`.bon-detail-empty`).

### Details verification (Postgres)

- `/Bon/Details/14` painted **Amba**, `25 Oktober 2026 · Belum lunas`, **Rp 4.500** / **Rp 0** / **Rp 4.500**, and both lines at the prices recorded on them (`Rp 2.500 × 1 pcs`, `Rp 2.000 × 1 pcs`).
- Two inserted payments painted **Cicilan 1** and **Cicilan 2** oldest first and moved sisa to **Rp 2.000**; a third one made the header read **Lunas**, the sisa **Rp 0**, and the sheet hint **Bon ini sudah lunas.** The test payments were deleted again.
- With no payment rows the history showed **Belum ada pembayaran.**
- The list card carries `/Bon/Details/14`, so a card opens its own page.
- `/Bon/Details/99999` answered `404`; the same call with `X-Requested-With: XMLHttpRequest` answered the `ErrorOr` JSON.
- `dotnet build` succeeded with 0 warnings and 0 errors.

---

## Slice 5: dynamic item picker

`/Bon/Create` no longer ships the whole stock list inside the page. The picker paints the first page that
`GetFormAsync` returns and asks `IBonService.SearchBarangAsync` for the rest while the shopkeeper types.
A rejected POST paints the lines it already picked again instead of starting over.

### File map

- Create: `Models/Bon/DTOs/BonLineDto.cs` (one repainted line: item id, name, unit, price, jumlah)
- Create: `Views/Bon/_BarangRows.cshtml` (picker rows plus the load-more button, mirrors `Views/Barang/_Cards.cshtml`)
- Modify: `Models/Bon/DTOs/BonFormDto.cs` (`Barangs` becomes `PagedResult<BonBarangOptionDto>`)
- Modify: `Models/Bon/ViewModels/CreateBonViewModel.cs` (`Barangs` type, new `[BindNever] Lines`)
- Modify: `Domain/Interfaces/IBonService.cs`, `Domain/Services/BonService.cs` (`SearchBarangAsync`,
  `ResolveLinesAsync`, private `ReadBarangPageAsync`, `BarangPageSize = 20`)
- Modify: `Controllers/BonController.cs` (`SearchBarang`, `Lines` refilled on both rejected POST paths)
- Modify: `Views/Bon/Create.cshtml` (`data-search-url`, `_BarangRows`, `#bonInitialLines`)
- Modify: `wwwroot/custom/features/bon/create.js` (debounced server search, load-more, initial lines)
- Modify: `wwwroot/css/site.css` (`.bon-picker-load-more`)

### Steps

- [x] **Step 1:** `SearchBarangAsync(string? keyword, int page)` matches the item name with the same
  `ILIKE` escape `SearchAsync` uses, orders by `Nama` then `Id`, pages **20** rows in the database, and
  returns `PagedResult<BonBarangOptionDto>` so the sheet knows whether another page follows.
- [x] **Step 2:** `GetFormAsync` returns the first picker page only, so the Create page carries at most
  20 items whichever way it is read.
- [x] **Step 3:** `SearchBarang(q, page)` answers `_BarangRows` for AJAX callers and the `ErrorOr` JSON
  for a failed lookup; a page request gets the same partial rather than a status code.
- [x] **Step 4:** `create.js` debounces the search box (250 ms), aborts the in-flight request, replaces
  only the sheet's rows so the box keeps focus and its text, and appends the next page through the
  button the server renders. Closing the sheet clears the term and loads page 1 again. Without
  `data-search-url` the sheet falls back to filtering the rows the page shipped.
- [x] **Step 5:** `ResolveLinesAsync` re-reads the posted barang ids from the stock list, and the
  controller fills `CreateBonViewModel.Lines` on both rejected POST paths. The view hands the lines to
  the script as JSON in `#bonInitialLines`, and `create.js` paints each one through `#bonLineTemplate`,
  so a validation error never drops what was already picked.
- [x] **Step 6:** `dotnet build` succeeds with 0 warnings and 0 errors.

### Verification (Postgres, 2026-10-31)

- `/Bon/Create` painted the 11 stock rows as the first page, carried `data-search-url="/Bon/SearchBarang"`,
  and `#bonInitialLines` read `[]`.
- `/Bon/SearchBarang?q=kopi` answered `200` with **kopi**, **kopi gooday**, and **kopi sachet**;
  `q=zzz` painted **Belum ada barang yang cocok. Coba kata kunci lain.**; `q=%` matched the wildcard
  literally (0 rows).
- With 25 temporary items on top of the real 11, the first page painted 20 rows and a **Muat barang
  berikutnya** button pointing at page 2; page 2 painted the remaining 5 and no button. The temporary
  rows were deleted again, leaving the 11 real items.
- A POST with two lines and no customer answered `200` with **Pelanggan wajib dipilih** and
  `#bonInitialLines` carrying both lines (`kopi gooday` x 2, `kopi` x 1), so the form repaints them.
- A second POST with the customer filled and a **Total Bayar** above the lines answered `200` with
  **Total bayar tidak boleh lebih dari total belanja** and `#bonInitialLines` carrying the line
  (`kopi gooday` x 3), so the service's rejection path repaints the form too; no `bon` row was written
  (the notebook still held `BN-001` only).
- A temporary stock item named `zz </script> test` came back as `zz \u003C/script\u003E test` inside
  `#bonInitialLines`, so a name cannot break out of the JSON block. The temporary item was deleted
  again, leaving the 11 real items.

---

## Open decisions

Both were locked on 2026-10-25:

1. **Total Bayar on Create** — locked: it becomes the first `bon_payment` row, so paid money has one source.
2. **Seed example data** — locked: no seed. `/Bon` starts on the empty state and the first real bon is `BN-001`.

## Follow-ups (not this plan)

- Record a payment from Details, which retires the dead **Catat pembayaran** link.
- Edit and delete a bon; the slicing specs have no design for either.
- Stock movement: a bon does not touch `barang` beyond reading its price.


> Update: adding a barang from Details is no longer a follow-up. The **Tambah barang** sheet lists the real
> stock list, searches and pages through `/Bon/SearchBarang`, and a pick writes the line through
> `POST /Bon/AddItem`. See `docs/plans/plan-tambah-barang-dinamis.md`.

> Update (2026-10-31): recording a payment from Details is no longer a follow-up either. The **Catat
> pembayaran** sheet posts `BonId`, `Jumlah`, `Tanggal`, and `Catatan` to `POST /Bon/AddPayment`, which
> writes one `bon_payment` row and redirects back to the page. See
> `docs/plans/plan-catat-pembayaran-dinamis.md`.

