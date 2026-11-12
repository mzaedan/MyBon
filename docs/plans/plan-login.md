# Login with two roles plan

**Goal:** Put every page behind a login. Accounts live in the local `pengguna` table (username + hashed password). There are exactly **two roles**:

- **Admin** — the shop owner. Full access: the whole app as it stands today (dashboard, customers, stock, bons, payments, deleting settled bons) **plus** account management. There is no separate cashier role — the owner is the cashier.
- **Pelanggan** — a customer with their own account, tied to exactly one `pelanggan` row. The only menu they reach is **Catat Bon** (`Bon/Index`, `Bon/Details`), and inside that menu they see **only their own bons**, read-only. No dashboard, no stock, no other customer's page, no write action anywhere.

**Depends on:** `Program.cs`, `Controllers/BaseController.cs`, `Data/AppDbContext.cs`, `Views/Shared/_Layout.cshtml`, tokens in `wwwroot/css/site.css`.

**Status:** planned, not implemented. No auth exists today — `Program.cs` calls `UseAuthorization()` without any authentication handler, and no controller carries an attribute.

**Library note:** cookie authentication (`Microsoft.AspNetCore.Authentication.Cookies`), `PasswordHasher<T>` (`Microsoft.Extensions.Identity.Core`) and the rate limiter all ship in the ASP.NET Core shared framework on this machine (`Microsoft.AspNetCore.App/10.0.14`). **No NuGet package is added.** Full ASP.NET Core Identity (its EF stores, its UI, `IdentityDbContext`) is deliberately not used.

## Permission matrix

| Screen / action | Admin | Pelanggan |
|---|---|---|
| `Dashboard/Index` | yes | no |
| `Pelanggan/*` (list, details, create, edit, delete) | yes | no |
| `Barang/*` (list, details, create, edit, delete) | yes | no |
| `Bon/Index`, `Bon/Search` | every bon | **own bons only** |
| `Bon/Details` — full page with the payment and item sheets | yes | own bon, read-only |
| `Bon/Create`, `Bon/SearchBarang`, `Bon/AddItem`, `Bon/AddPayment`, `Bon/Delete` | yes | no |
| `Pengguna/*` (account management, including creating a customer account) | yes | no |
| `Auth/GantiPassword` (own password) | yes | yes |

Rationale: the owner runs the shop and holds every key. A customer shares the **Catat Bon** menu with the owner — same route, same cards, same detail page — but the list query is filtered by `pengguna.pelanggan_id` in the database and every write entry point is gone. The customer side must never expose another customer's page, the stock price list, or any write endpoint.

## Landing after login

| Role | Redirect |
|---|---|
| Admin | `Dashboard/Index` |
| Pelanggan | `Bon/Index` |

`ReturnUrl` still wins over both when it is local (`Url.IsLocalUrl`), so a deep link survives the login.

## Architecture

```
GET  /Auth/Login                 -> Views/Auth/Login.cshtml (anonymous, _LayoutAuth)
POST /Auth/Login                 -> AuthService.MasukAsync -> cookie (claims) -> redirect by role
POST /Auth/Logout                -> SignOutAsync -> /Auth/Login
GET  /Auth/Ditolak               -> "no access" page for a signed-in user without the role
GET  /Auth/GantiPassword         -> own password, both roles

fallback policy: RequireAuthenticatedUser
[Authorize(Roles = Peran.Admin)]   -> class of DashboardController, PelangganController,
                                        BarangController, PenggunaController
[Authorize]                          -> class of BonController (the shared Catat Bon menu)
[Authorize(Roles = Peran.Admin)]   -> BonController.Create (GET + POST), SearchBarang, AddItem,
                                        AddPayment, Delete, DeleteConfirmed
[AllowAnonymous]                     -> AuthController, Home/Error

claim id   -> ClaimTypes.NameIdentifier   (pengguna.id)
claim name -> ClaimTypes.Name             (pengguna.nama)
claim role -> ClaimTypes.Role             (PeranPengguna, stored as text)
claim usr  -> "usr"                       (pengguna.username, matches AGENTS.md)
claim pid  -> "pid"                       (pengguna.pelanggan_id, only for the Pelanggan role)
```

Guard at **controller level** for the back office: `Dashboard`, `Pelanggan`, `Barang` and `Pengguna` belong to the owner alone, so one attribute per class is easier to audit than twenty scattered over actions. `BonController` is the one shared controller — `[Authorize]` on the class lets both roles in, and its six write actions carry `[Authorize(Roles = Peran.Admin)]` on top. Attributes are the security boundary; `ViewData["CanWriteBon"]` only decides whether the create CTA, the action bar and the two sheets are painted.

`IContext` / `IIdentityContext` finally get introduced here, because role checks inside views and services must not read `HttpContext.User` (AGENTS.md rule). `ContextMiddleware` fills a scoped `Context` from the cookie claims once per request, and exposes `Identity.PelangganId` for the customer screens.

The customer's `pelanggan_id` comes from the **cookie claim, never from the query string**, and every customer query filters on it in the database. Asking for someone else's bon answers `NotFound` (not `Forbidden`), so the page cannot be used to probe which bon ids exist. A customer account whose claim is missing is answered as no access — never as "show everything". The customer path also leaves `BonDetailDto.Barangs` empty, so the stock picker's rows never travel to a browser that cannot use them.

Denial must not answer JSON callers with a 302: the cookie events (`OnRedirectToLogin`, `OnRedirectToAccessDenied`) return `401` / `403` with an `ErrorOr` body when the request carries `X-Requested-With: XMLHttpRequest` or `Accept: application/json`, and redirect otherwise. Every list page here is AJAX driven, so a redirect would land inside a partial update.

## Database

Table `pengguna` (snake_case, like the other tables):

| Column | Type | Notes |
|---|---|---|
| `id` | int identity | PK |
| `username` | varchar(50) | required, **unique index** |
| `nama` | varchar(200) | required |
| `password_hash` | varchar(500) | required, never plaintext |
| `peran` | varchar(20) | required, `HasConversion<string>()` |
| `pelanggan_id` | int? | FK → `pelanggan.id`, `ON DELETE CASCADE`, **unique index** (one account per customer), `NULL` for the owner |
| `aktif` | boolean | default `true` |
| `terakhir_masuk` | timestamptz | nullable |
| `created_at` | timestamptz | default `CURRENT_TIMESTAMP` |

Check constraint `ck_pengguna_peran_pelanggan`: `peran <> 'Pelanggan' OR pelanggan_id IS NOT NULL`, declared through `entity.ToTable("pengguna", table => table.HasCheckConstraint(...))` the way `bon_item` already does it. The database refuses a customer account that belongs to nobody.

Migration: `dotnet ef migrations add CreatePenggunaTable` (Design/Tools packages are already referenced).

## File map

**Create**

- `Domain/Enums/PeranPengguna.cs` — `{ Admin, Pelanggan }`
- `Domain/Constants/Peran.cs` — `const string` mirrors used by `[Authorize(Roles = ...)]`
- `Domain/Entities/Pengguna.cs` — `PelangganId` + `Pelanggan?` navigation
- `Domain/Interfaces/IAuthService.cs`, `Domain/Interfaces/IPenggunaService.cs`
- `Domain/Services/AuthService.cs`, `Domain/Services/PenggunaService.cs`
- `Domain/Abstractions/IContext.cs`, `Domain/Abstractions/IIdentityContext.cs`
- `Infrastructure/Contexts/Context.cs`
- `Infrastructure/Middleware/ContextMiddleware.cs`
- `Infrastructure/Persistence/PenggunaSeeder.cs`
- `Models/Auth/Requests/LoginRequest.cs`, `GantiPasswordRequest.cs`
- `Models/Auth/Requests/CreatePenggunaRequest.cs`, `ResetPasswordRequest.cs`
- `Models/Auth/ViewModels/PenggunaListViewModel.cs`
- `Controllers/AuthController.cs`, `Controllers/PenggunaController.cs`
- `Views/Auth/Login.cshtml`, `Views/Auth/Ditolak.cshtml`, `Views/Auth/GantiPassword.cshtml`
- `Views/Shared/_LayoutAuth.cshtml`
- `Views/Pengguna/Index.cshtml`, `Views/Pengguna/Create.cshtml`
- `wwwroot/custom/features/auth/login.js`
- `wwwroot/custom/features/pengguna/index.js`
- `Migrations/xxxx_CreatePenggunaTable.cs` (generated)

**Modify**

- `Data/AppDbContext.cs` — `DbSet<Pengguna>` + `OnModelCreating` block (columns, unique indexes, check constraint, cascade)
- `Program.cs` — cookie authentication, authorization fallback, rate limiter, `IAuthService`/`IPenggunaService`/`IPasswordHasher<Pengguna>`/`IContext` registrations, `UseAuthentication()`, `UseMiddleware<ContextMiddleware>()`, `UseRateLimiter()`, seeder call
- `Controllers/BaseController.cs` — lazy `Context` property + `IsAdmin` helper
- `Controllers/DashboardController.cs`, `PelangganController.cs`, `BarangController.cs`, `BonController.cs` — `[Authorize(Roles = Peran.Admin)]` on the class
- `Domain/Interfaces/IBonService.cs` + `Domain/Services/BonService.cs` — `SearchMineAsync`, `GetMineDetailAsync` filtered by `pelangganId`
- `Views/Bon/Index.cshtml` — both **Catat bon baru** CTAs, the empty-state copy and the search placeholder follow `ViewData["CanWriteBon"]`
- `Views/Bon/Details.cshtml` — action bar, both sheets and the sheet scripts follow `ViewData["CanWriteBon"]`
- `Controllers/PelangganController.cs` — `Details` flag telling the view whether the customer already has an account
- `Views/Shared/_Layout.cshtml` — nav per role, signed-in name + role, **Keluar** (POST form + antiforgery), gear link to `/Pengguna` for the owner
- `Views/Pelanggan/Details.cshtml` — **Buatkan akun** button for a customer without one
- `Views/_ViewImports.cshtml` — `@using MyBon.Domain.Abstractions`, `@using MyBon.Models.Auth.Requests`, `@using MyBon.Domain.Enums`
- `wwwroot/css/site.css` — login screen, header actions, user chip, one-item nav variant
- `appsettings.json` — `Auth` section (cookie lifetime); the seed password stays in user-secrets

## Slice 1: schema and the first account

- [ ] **Step 1:** `PeranPengguna { Admin, Pelanggan }`, and `Domain/Constants/Peran.cs` holding `public const string Admin = nameof(PeranPengguna.Admin);` plus the same for `Pelanggan`, because `[Authorize(Roles = ...)]` needs a compile-time constant.
- [ ] **Step 2:** `Pengguna : BaseEntity` with `Id`, `Username`, `Nama`, `PasswordHash`, `Peran`, `PelangganId?`, `Pelanggan?`, `Aktif`, `TerakhirMasuk?`. No data annotations on the entity.
- [ ] **Step 3:** `AppDbContext`: `DbSet<Pengguna>`; `ToTable("pengguna", table => table.HasCheckConstraint("ck_pengguna_peran_pelanggan", "peran <> 'Pelanggan' OR pelanggan_id IS NOT NULL"))`; snake_case columns; `HasMaxLength` on `username` (50), `nama` (200), `password_hash` (500), `peran` (20); `HasConversion<string>()` on `Peran`; unique index on `username`; unique index on `pelanggan_id`; `HasOne(p => p.Pelanggan).WithMany().HasForeignKey(p => p.PelangganId).OnDelete(DeleteBehavior.Cascade)`; `aktif` default `true`; `created_at` default `CURRENT_TIMESTAMP`.
- [ ] **Step 4:** `dotnet ef migrations add CreatePenggunaTable` then `dotnet ef database update` (Postgres from `docker-compose.yml`, port 5439).
- [ ] **Step 5:** `PenggunaSeeder.SeedAsync` inserts the owner only when the table has no row: username `admin`, `Peran = Admin`, `PelangganId = null`, password from `Auth:SeedPassword` (user-secrets / `Auth__SeedPassword`) hashed with `IPasswordHasher<Pengguna>`. A missing password skips seeding with a warning instead of inventing one.
- [ ] **Step 6:** Call the seeder from `Program.cs` inside a scope after `builder.Build()`.

## Slice 2: sign in and sign out

- [ ] **Step 1:** `LoginRequest` (`Username` required, `Password` required + `DataType.Password`, `IngatSaya`, `ReturnUrl`) with Indonesian messages, matching `CreatePelangganRequest` style.
- [ ] **Step 2:** `AuthService.MasukAsync` looks the account up by username (including `Pelanggan`), refuses `Aktif == false`, verifies with `IPasswordHasher.VerifyHashedPassword`, and stamps `TerakhirMasuk` on success. **Every failure returns the same** `"Username atau password salah"` with `ErrorType.Unauthorized` — no hint about which half was wrong.
- [ ] **Step 3:** Re-hash when `PasswordVerificationResult.SuccessRehashNeeded` comes back, so an old hash upgrades on the next successful sign-in.
- [ ] **Step 4:** `AuthController.Login` (GET) renders the form and keeps `ReturnUrl` from the query string.
- [ ] **Step 5:** `AuthController.Login` (POST) is `[ValidateAntiForgeryToken]`, calls the service, and on failure uses `ToActionResult(result, onSuccess, onFailure)` with `AddErrorsToModelState(failure)` + `View(model)` so the message sits under the fields. On success it writes the claims (`id`, `name`, `role`, `usr`, and `pid` for a customer) and redirects: local `ReturnUrl` first, else `Dashboard/Index` for the owner and `Bon/Index` for a customer.
- [ ] **Step 6:** `AuthController.Logout` is POST-only, `[ValidateAntiForgeryToken]`, calls `SignOutAsync` and redirects to `Auth/Login`.
- [ ] **Step 7:** `Program.cs`: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(...)` with `LoginPath = "/Auth/Login"`, `LogoutPath = "/Auth/Logout"`, `AccessDeniedPath = "/Auth/Ditolak"`, `ExpireTimeSpan = 8h`, `SlidingExpiration = true`, `Cookie.HttpOnly = true`, `Cookie.SameSite = Lax`, `Cookie.SecurePolicy = SameAsRequest`; **Ingat saya** sets `IsPersistent` with a 30-day `ExpiresUtc`.
- [ ] **Step 8:** `AddAuthorization` with `FallbackPolicy = RequireAuthenticatedUser`; `AuthController` carries `[AllowAnonymous]` and `Home/Error` stays anonymous so a broken request still renders.
- [ ] **Step 9:** Insert `app.UseAuthentication()` before `app.UseAuthorization()` (missing today).
- [ ] **Step 10:** `Views/Auth/Login.cshtml` on a new `_LayoutAuth.cshtml`: wordmark **MY BON**, no bottom nav, no back button, username + password rows, **Ingat saya**, black **Masuk** button, error banner. Reuses the existing CSS tokens and form row classes from `site.css`.
- [ ] **Step 11:** `wwwroot/custom/features/auth/login.js`: show/hide password toggle and a disabled-while-submitting button. Registered through `@section Scripts` with `asp-append-version`; no new JS library.

## Slice 3: the signed-in user context

- [ ] **Step 1:** `IIdentityContext` (`Id`, `Username`, `FullName`, `Email?`, `Peran`, `PelangganId`, `IsAuthenticated`, `IsInRole(string)`) and `IContext` (`Identity`) in `Domain/Abstractions/`.
- [ ] **Step 2:** `Infrastructure/Contexts/Context.cs` reads the claims once in its constructor.
- [ ] **Step 3:** `Infrastructure/Middleware/ContextMiddleware.cs` puts the scoped `Context` on `HttpContext.Items` so services inject `IContext` normally.
- [ ] **Step 4:** `BaseController` gains a lazy `Context` property (no `IContext` in controller constructors) plus `protected bool IsAdmin => Context?.Identity.IsInRole(Peran.Admin) == true;`, which every shared screen uses to set `ViewData["CanWriteBon"]`.
- [ ] **Step 5:** `Views/_ViewImports.cshtml` adds `@using MyBon.Domain.Abstractions` so a view can `@inject IContext Context` when it needs the name or role.

## Slice 4: lock the back office to the owner

- [ ] **Step 1:** `[Authorize(Roles = Peran.Admin)]` on the class of `DashboardController`, `PelangganController`, `BarangController` and `PenggunaController`. One attribute closes every action in each of them, including the AJAX `Search` endpoints their lists call.
- [ ] **Step 2:** `BonController` keeps `[Authorize]` on the class (both roles reach the menu) and gains `[Authorize(Roles = Peran.Admin)]` on `Create` (GET + POST), `SearchBarang`, `AddItem`, `AddPayment`, `Delete` (GET) and `DeleteConfirmed` — the six write entry points plus the stock picker.
- [ ] **Step 3:** `HomeController.Error` gets `[AllowAnonymous]`; `Home/Privacy` stays behind the fallback policy.
- [ ] **Step 4:** Cookie events: `OnRedirectToLogin` / `OnRedirectToAccessDenied` answer `401` / `403` with an `ErrorOr` JSON body for `X-Requested-With: XMLHttpRequest` or `Accept: application/json`, and redirect otherwise. Without this an AJAX call answers a 302 and the partial never updates.
- [ ] **Step 5:** `Views/Auth/Ditolak.cshtml`: signed in but wrong role, with a link back to the dashboard (owner) or to **Catat Bon** (customer).
- [ ] **Step 6:** `Views/Shared/_Layout.cshtml`: the bottom nav renders **Dashboard / Pelanggan / Barang / Catat Bon** for the owner and a single **Catat Bon** entry (`Bon/Index`) for a customer, reading `Context.Identity.Peran`; the header gains the signed-in name + role chip, a **Keluar** POST form with antiforgery, and a gear link to `/Pengguna` for the owner only. The 4-slot nav geometry stays untouched.
- [ ] **Step 7:** `wwwroot/css/site.css`: the one-item nav variant centers its single item at the width of a four-slot cell instead of letting the label stretch across the bar.

## Slice 5: the Catat Bon menu for a customer

- [ ] **Step 1:** `IBonService.SearchMineAsync(int pelangganId, string? keyword, int page)` — the existing list query with `b.PelangganId == pelangganId` added, same paging, ordering and total, returning `PagedResult<BonListItemDto>`.
- [ ] **Step 2:** `IBonService.GetMineDetailAsync(int bonId, int pelangganId)` — the existing detail read with the ownership check inside the query; a bon belonging to someone else returns `ErrorType.NotFound` with the same message as a bon that does not exist.
- [ ] **Step 3:** `BonController.Index`/`Search` branch on the role: a customer calls `SearchMineAsync` with `Context.Identity.PelangganId`, the owner keeps `SearchAsync`. A customer account without a `PelangganId` is answered as no access (403), never as the whole notebook.
- [ ] **Step 4:** `BonController.Details` sets `ViewData["CanWriteBon"] = Context.Identity.IsInRole(Peran.Admin)` and reads the bon through `GetMineDetailAsync` for a customer, through `GetDetailAsync` for the owner; the customer path leaves `Barangs` empty so the picker partial has nothing to paint.
- [ ] **Step 5:** `Views/Bon/Details.cshtml`: wrap the action bar (Hapus bon / Catat pembayaran / Tambah barang) and the `@if (!Model.Lunas)` sheets block in `@if (canWrite && ...)`, and render the `@section Scripts` (`bon-calendar.js`, `money-input.js`, `details.js`) only when `canWrite`. Meta, Ringkasan, Riwayat pembayaran and Daftar barang stay shared, so both roles read identical markup and no tuned owner layout is duplicated.
- [ ] **Step 6:** `Views/Bon/Index.cshtml`: **Catat bon baru** renders only for the owner; for a customer the empty state reads `Belum ada bon untuk kamu.` and the search placeholder becomes `Cari nomor bon` (their own name is not worth searching).
- [ ] **Step 7:** `_Cards.cshtml` stays as it is — `NamaPelanggan` is the customer's own name and the card still links to `Bon/Details`, so a customer opens their own page from the same card. Verify the load-more button keeps calling `Bon/Search`, which now returns only their rows.

Rejected alternative: a separate read-only detail view for customers. It would duplicate roughly ninety lines of tuned summary/history/item markup and let the two copies drift; a single view with one flag keeps the owner page untouched in behaviour and the customer page identical in shape.


## Slice 6: account management for the owner

- [ ] **Step 1:** `IPenggunaService`/`PenggunaService`: `DaftarAsync`, `BuatAdminAsync`, `BuatAkunPelangganAsync(int pelangganId, ...)`, `ResetPasswordAsync`, `SetAktifAsync`, `GantiPasswordAsync(own)` — all return `ErrorOr` and never expose `PasswordHash`.
- [ ] **Step 2:** Guards as business errors, not exceptions: username already taken → `ErrorType.Conflict`; customer already has an account → `Conflict`; unknown account or customer → `NotFound`; deactivating the last active **Admin** → `Conflict`; an account may not deactivate itself.
- [ ] **Step 3:** `PenggunaController` with `[Authorize(Roles = Peran.Admin)]`: `Index`, `Create` (GET/POST, optional `?pelangganId=`), `ResetPassword` (POST), `SetAktif` (POST) — all POSTs `[ValidateAntiForgeryToken]`.
- [ ] **Step 4:** `Views/Pengguna/Index.cshtml` + `Create.cshtml` + `wwwroot/custom/features/pengguna/index.js` in the existing mobile shell; the list shows name, username, role, active state and last sign-in, never the hash.
- [ ] **Step 5:** `Pelanggan/Details` shows **Buatkan akun** for a customer without one and an "sudah punya akun" note otherwise; the button links to `Pengguna/Create?pelangganId=`.
- [ ] **Step 6:** `AuthController.GantiPassword` (GET/POST) so both roles can change their own password; the POST verifies the old password and signs the user in again with fresh claims.

## Slice 7: hardening and verification

- [ ] **Step 1:** Rate limiter: fixed window of 5 login POSTs per 5 minutes per client IP, `.RequireRateLimiting("login")` on `AuthController.Login` (POST), `app.UseRateLimiter()` in the pipeline.
- [ ] **Step 2:** Never log the password or the hash; a failed attempt logs the username only.
- [ ] **Step 3:** `appsettings.json` carries the `Auth` section (cookie lifetime); the seed password lives in user-secrets only.
- [ ] **Step 4:** Keep `UseHttpsRedirection` in front of everything; the cookie stays `HttpOnly` and `Lax`.

## Verification

Planned checks on this machine (`dotnet run`, Postgres from `docker-compose.yml`, headless Chrome over CDP):

- [ ] `dotnet build` — 0 warnings, 0 errors.
- [ ] `node --check wwwroot/custom/features/auth/login.js` and `.../pengguna/index.js` pass.
- [ ] Table `pengguna` exists with unique indexes on `username` and `pelanggan_id`; the check constraint rejects `peran = 'Pelanggan'` with a null `pelanggan_id`; the seeded owner holds a PBKDF2 hash, not the plaintext.
- [ ] `GET /Bon` with no cookie → `302` to `/Auth/Login?ReturnUrl=%2FBon`; after signing in as the owner the browser lands back on `/Bon`.
- [ ] Wrong password → form re-renders with **Username atau password salah**, HTTP 200, no cookie. An inactive account answers the same message.
- [ ] `ReturnUrl=https://example.com` is ignored; the owner lands on `Dashboard/Index` (no open redirect).
- [ ] `POST /Auth/Login` without an antiforgery token → 400.
- [ ] **Admin:** every screen and every write action works exactly as today; the header offers `/Pengguna` and **Keluar**.
- [ ] **Pelanggan:** signing in lands on `/Bon`; the nav shows **Catat Bon** only; `/Dashboard`, `/Pelanggan`, `/Barang`, `/Pengguna` all answer the 403 page.
- [ ] `GET /Bon` as a customer lists only that customer's bons, and `GET /Bon/Search?page=2` stays inside them; `GET /Bon/Details/<bon milik pelanggan lain>` → 404 with the same body as a missing bon.
- [ ] The customer's `Bon/Details` paints no **Hapus bon**, **Catat pembayaran** or **Tambah barang**, and its HTML carries no `bon-add-list` rows or sheet scripts.
- [ ] A customer POSTing to `/Bon/AddPayment`, `/Bon/AddItem`, `/Bon/Create` or `/Barang/Delete` is refused (403 page; `403` + `ErrorOr` JSON with `X-Requested-With: XMLHttpRequest`).
- [ ] Creating a second account for the same customer → `Conflict`, not a database exception.
- [ ] Deactivating the last active owner → `Conflict`; deactivating a customer account blocks that account's next sign-in with the generic message.
- [ ] `POST /Auth/Logout` clears the cookie and the next page request redirects to login; a GET on `/Auth/Logout` is refused.
- [ ] Cookie flags: `HttpOnly`, `SameSite=Lax`, `Path=/`; **Ingat saya** adds an expiry about 30 days out.
- [ ] Six failed logins in a row answer 429 for the rest of the window.

## Non-goals

A separate cashier role (the owner already covers it), a dedicated `/BonSaya` route or customer menu (the customer shares **Catat Bon**), customer self-registration, customers recording their own payments or disputing a bon, email/WhatsApp notifications, external auth service (`AGENTS.md` §Auth Flow stays as documentation for a later switch), full ASP.NET Core Identity, email-based password reset, 2FA, per-user audit columns on `bon`/`bon_payment`, wrapper view models for the existing list pages, moving controllers into `Areas/`, switching the database to SQL Server.
## Implementation status (2026-11-12)

Implemented end to end. Decisions taken while building that refine the plan above:

- **Role name is `Admin`, not `Pemilik`.** Renamed across this document, the enum, the constants,
  the claims, the check constraint, and the UI.
- **Schema is minimal: one new table, `pengguna`.** No `log_aktivitas`, no `peran`/`pengguna_peran`,
  no audit columns on `bon`/`bon_payment`. Migration `20261112132753_CreatePenggunaTable` with both
  check constraints (`ck_pengguna_peran`, `ck_pengguna_peran_pelanggan`) and the two unique indexes.
- `pengguna.username` is trimmed and stored lowercase, so sign-in is case-insensitive under the
  unique index.
- `IIdentityContext.Peran` is `PeranPengguna?`; an anonymous or unparsable claim is never treated as
  Admin by accident.
- Account management uses one `IPenggunaService.BuatAsync(CreatePenggunaRequest)`: an empty
  `PelangganId` creates an admin account, a value creates that customer's account. There is no
  separate `BuatAdminAsync`/`BuatAkunPelangganAsync`.
- `IPenggunaService` returns `PenggunaDto`, never the entity, so `password_hash` cannot reach a view
  or a JSON response.
- Cookie lifetime (8 h, 30 days with **Ingat saya**) lives as constants in `AuthController`; no
  `Auth` section was added to `appsettings.json`. The seed password lives in user-secrets
  (`Auth:SeedPassword`) and the seeder skips with a warning when it is missing.
- Rate limiting uses `[EnableRateLimiting("login")]` on the POST action (the MVC form of the policy):
  5 attempts / 5 minutes / client IP, `429` afterwards.
- `AuthController` is not `[AllowAnonymous]` at class level: that attribute would have overridden the
  `[Authorize]` on `Ditolak` and `GantiPassword`. Only the two `Login` actions are anonymous.
- The login screen no longer offers **Buat akun toko baru**; accounts are created by an admin.
- `HomeController` still does not inherit `BaseController`; only `[AllowAnonymous]` was added to `Error`.
- `app.MapStaticAssets().AllowAnonymous()` — static assets are endpoints, so the authorization
  fallback policy covered them too. Without the attribute an anonymous browser asking for
  `/css/site.css` received `302 /Auth/Login?ReturnUrl=%2Fcss%2Fsite.css` and the sign-in screen
  painted with no stylesheet, no Bootstrap, and no icons. Stylesheets, scripts, and images stay
  public; every Razor page is still guarded.
- Known limitation: disabling an account blocks its **next** sign-in but does not invalidate a cookie
  already issued (no security stamp). Such a cookie lives at most 8 hours, or 30 days with
  **Ingat saya**.

Verified on this machine (HTTP, Postgres from `docker-compose.yml`):

- `dotnet build` — 0 warnings, 0 errors.
- `dotnet ef database update` — `pengguna` created with both check constraints, the unique
  `username` index, and the unique `pelanggan_id` index.
- `GET /Bon` anonymous → `302 /Auth/Login?ReturnUrl=%2FBon`; wrong password → `200` with
  **Username atau password salah** and no cookie.
- Admin lands on `Dashboard/Index`; customer lands on `Bon/Index`.
- Customer `/Bon` lists only their own bons; another customer's `/Bon/Details/<id>` → `404`; their
  own page paints no action bar, no sheets, and no sheet scripts.
- Customer `POST /Bon/AddPayment` and `POST /Bon/Create` are refused; AJAX refusals answer `403`
  with an `ErrorOr` body, anonymous AJAX answers `401`.
- Admin still sees every bon, the create CTA, and the write controls; `/Pengguna` renders no hash.
- Deactivating the signed-in admin is refused; an inactive customer's sign-in answers the generic
  message; reactivating restores it. A duplicate account for one customer answers `Conflict`.
- `POST /Auth/Logout` clears the cookie; `GET /Auth/Logout` is not routed; six sign-in attempts in
  the window answer `429`.
