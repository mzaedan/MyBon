# Bon dinamis plan

**Goal:** Make **Catat Bon** read and write the notebook. `/Bon` lists real bons, `/Bon/Create` saves a header plus its item lines, and `/Bon/Details/{id}` reads one bon back. Today all three views are hardcoded markup and `BonController` never touches the database.

**Status:** schema agreed, implementation not started. Two decisions in [Open decisions](#open-decisions) are still unconfirmed.

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

- Create: `Domain/Entities/Bon.cs`, `BonItem.cs`, `BonPayment.cs`
- Modify: `Data/AppDbContext.cs` (`DbSet`s, `HasSequence("bon_nomor_seq")`, snake_case mapping)
- Create: `Migrations/<timestamp>_CreateBonTables.cs` (via `dotnet ef migrations add CreateBonTables`)
- Create: `Domain/Interfaces/IBonService.cs`, `Domain/Services/BonService.cs`
- Create: `Models/Bon/Requests/CreateBonRequest.cs`, `Models/Bon/DTOs/BonListItemDto.cs`, `Models/Bon/DTOs/BonDetailDto.cs`
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
| `harga_satuan` | `numeric(14,2)` | no | Snapshot at recording time |
| `jumlah` | `integer` | no | Whole units only, default `1`, must be >= 1 |
| `created_at` | `timestamptz` | no | `BaseEntity`; this is the per-line date Details prints |

### `bon_payment` — payment history

| Column | Type | Null | Notes |
|---|---|---|---|
| `id` | `integer` identity | no | PK |
| `bon_id` | `integer` | no | FK -> `bon.id`, `OnDelete(Cascade)` |
| `tanggal` | `date` | no | Payment date |
| `jumlah` | `numeric(14,2)` | no | Must be > 0 |
| `keterangan` | `varchar(100)` | yes | **Cicilan 1** style labels are rendered, not stored |
| `created_at` | `timestamptz` | no | `BaseEntity` |

### Numbering

A dedicated sequence keeps `BN-001` sequential and safe when two tabs save at once, with no `MAX(nomor) + 1` race and no extra round trip: Postgres assigns the value and EF reads it back through `RETURNING`.

```csharp
modelBuilder.HasSequence<int>("bon_nomor_seq").StartsAt(1).IncrementsBy(1);

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
| `bon(pelanggan_id)` | a customer's bons |
| `bon(tanggal desc, id desc)` | list order |
| `bon_item(bon_id)` | lines of one bon |
| `bon_item(barang_id)` | usage check before an item is deleted |
| `bon_payment(bon_id)` | payment history of one bon |

- [ ] **Step 1:** Add the three entities, inheriting `BaseEntity`, FK plus nullable navigation on the same block (`PelangganId` + `Pelanggan?`), collections initialised with `[]`.
- [ ] **Step 2:** Map them in `AppDbContext.OnModelCreating` with the column names, lengths, `HasPrecision(14, 2)` on money, and the sequence above.
- [ ] **Step 3:** `dotnet ef migrations add CreateBonTables` then `dotnet ef database update`; read the generated migration and confirm the sequence, the default expression and the FK delete behaviours match the tables above.
- [ ] **Step 4:** Confirm `PelangganService.DeleteAsync` now hits the FK restriction for a customer who owns a bon, and return `ErrorType.Conflict` with **Pelanggan masih punya bon** instead of the generic save failure.

---

## Slice 2: read

- [ ] **Step 1:** `IBonService.SearchAsync(string? keyword, int page)` mirrors `BarangService`: `ILIKE` over `bon.nomor` and `pelanggan.nama`, `OrderByDescending(Tanggal).ThenByDescending(Id)`, `PageSize = 8`, returns `PagedResult<BonListItemDto>` carrying the numbers `/Bon` paints.
- [ ] **Step 2:** `GetByIdAsync(int id)` includes `Pelanggan`, `Items` and `Payments` (`AsNoTracking` + `AsSplitQuery`) and returns `BonDetailDto` with totals, `SisaHutang` and the status label computed.
- [ ] **Step 3:** `Index` asks page 1; `Search(q, page = 1)` answers an XHR with `_Cards`; `Details(int id)` sets `ViewData["Title"]` to the nomor and `ViewData["BackHref"]` to `Index`.
- [ ] **Step 4:** `/Bon/Details/99999` returns 404 through `ToActionResult`, and an AJAX call returns the `ErrorOr` JSON.

## Slice 3: write

- [ ] **Step 1:** `CreateBonRequest`: `PelangganId` (required), `Tanggal` (required), `Catatan`, `TotalBayar` as text parsed like `CreateBarangRequest.Harga`, and `List<CreateBonItemRequest>` (`BarangId`, `Jumlah`).
- [ ] **Step 2:** `CreateAsync` validates: customer exists, at least one line, every line has an existing barang, `jumlah >= 1`, `0 <= total bayar <= total belanja`. Prices are read from `barang`, never from the client, so a tampered page cannot set a price.
- [ ] **Step 3:** Save header and lines in one transaction; a total bayar above zero also writes the first `bon_payment` row, so `bon_payment` stays the only source of paid amounts.
- [ ] **Step 4:** `Create` GET loads the customer list and the item list for the customer sheet and the item picker; `Create` POST uses `[ValidateAntiForgeryToken]`, and a failure re-renders the form through `AddErrorsToModelState`.
- [ ] **Step 5:** On success `TempData["BonStatus"]` carries **Bon BN-0xx tersimpan** and the action redirects to `Index`.

## Slice 4: views and JS

- [ ] **Step 1:** `Index.cshtml` renders `_Cards` in a loop, deletes the four hardcoded cards, keeps `.bon-card*`, `.bon-search*` and `.cta-catat-bon`, and gains the empty state the Barang list has.
- [ ] **Step 2:** `Create.cshtml` turns `method="get"` into a POST form with an antiforgery token, fills the customer rows in `#bonCustomerPicker` from the model (name, phone, sisa), keeping `data-nama` on each `.bon-customer-item`, and replaces the hardcoded picker buttons with the items the page carries (`data-id`, `data-nama`, `data-satuan`, `data-harga`), so the hidden field submits the id.
- [ ] **Step 3:** `create.js` posts `Items[0].BarangId` / `Items[0].Jumlah` so model binding fills the list; the existing subtotal and total arithmetic stays.
- [ ] **Step 4:** `Details.cshtml` takes `BonDetailDto`: header from `Nomor` and `Pelanggan.Nama`, summary from the computed totals, history from `Payments`, lines from `Items` with `harga_satuan x jumlah satuan`. **Catat pembayaran** stops pointing at `Index` and waits for its own slice.

---

## Open decisions

1. **Total Bayar on Create** — recommended: it becomes the first `bon_payment` row, keeping one source for paid money. Alternative: a `total_bayar` column on `bon`, which means two places hold the same number.
2. **Seed example data** — recommended: no seed, `/Bon` shows the empty state until the first real bon. Alternative: seed `BN-001`..`BN-004` from the current hardcoded cards so the screens still look populated.

## Follow-ups (not this plan)

- Record a payment from Details, which retires the dead **Catat pembayaran** link.
- Edit and delete a bon; the slicing specs have no design for either.
- Stock movement: a bon does not touch `barang` beyond reading its price.

