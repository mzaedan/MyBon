# MyBon

## Project Overview
- **Framework:** .NET 10.0 ASP.NET Core MVC + Clean Architecture
- **ORM:** Entity Framework Core 10 + SQL Server
- **Pattern:** MVC + Domain-Driven Design

> **Before implementing any feature**, check `docs/plans/` for the relevant plan file. Do not implement features that contradict the plan.

> **When creating a new plan file**, always save it to `docs/plans/` with the naming format `plan-[feature-name].md` (e.g. `docs/plans/plan-incoming.md`). Never save plan files outside this directory.

---

## Stack

### Backend
- **Runtime:** .NET 10.0
- **ORM:** Entity Framework Core 10 + SQL Server
- **Scheduler:** Quartz.NET 3.8.1 (clustered, persistent)
- **Security overrides:** SixLabors.ImageSharp 2.1.11 is referenced directly to override NPOI's vulnerable transitive version.

### Frontend
- **UI Framework:** Bootstrap 5 + Metronic 8 (theme)
- **JS:** jQuery (bundled via Metronic) — do not import new JS libraries without confirmation

---

## Commands

### Build & Run
```bash
dotnet build                 # Build the project
dotnet run                   # Start the application
dotnet run --no-build        # Run without rebuilding
dotnet watch run             # Development with hot reload
```

### Database Migrations
```bash
dotnet ef migrations add <Name>        # Create new migration
dotnet ef migrations add <Name> --output-dir Infrastructure/Persistence/Migrations
dotnet ef database update              # Apply migrations
dotnet ef database update <Migration>  # Apply to specific migration
dotnet ef migrations remove            # Remove last migration
```

### Testing
- **No test project found** - tests are not yet configured
- To add tests: create `MyBon.Tests.csproj` with xUnit/NUnit

---

## Code Style Guidelines

### Imports
- Group by: `System` → `Microsoft` → `Third-party` → `Project`
- Remove unused imports before committing
- Use explicit namespaces, avoid `using static` unless necessary
- **Example:**
```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyBon.Domain.Entities;
using MyBon.Domain.Interfaces;
```

### Formatting
- Use 4 spaces for indentation (default in .editorconfig if present)
- Keep lines under 120 characters when practical
- Add blank line between method groups and logical sections
- Use expression-bodied members for simple getters
- **Example:**
```csharp
// Good
public string FullName => $"{FirstName} {LastName}";
public bool IsActive => !IsDeleted;

// Avoid
public string getFullName() { return FirstName + " " + LastName; }
```

### Code Naming Conventions
| Type | Convention | Example |
|------|-----------|---------|
| Class/Interface | PascalCase | `ApprovalSettingController` |
| Method | PascalCase | `GetByIdAsync` |
| Private field | _camelCase | `_dbContext` |
| Parameter | camelCase | `request` |
| Property | PascalCase | `UserId` |
| Constant | PascalCase | `MaxLevelCount` |
| Enum | PascalCase | `ApprovalStatus` |
| Folder/Directory | kebab-case | `wwwroot/custom/features/` |

### Types
- Use explicit types over `var` for public APIs and clarity
- Use `var` for local variables when type is obvious
- Nullable reference types: use `?` syntax (nullable enabled in project)
- **Example:**
```csharp
// Good
private readonly AppDbContext _dbContext = dbContext;
public async Task<IActionResult> GetByIdAsync(Guid id)

// Avoid
var dbContext = new AppDbContext();
public async Task< IActionResult > GetByIdAsync(System.Guid id)
```

### Comments
- **Language:** English only
- `///` XML doc on all public interfaces and non-obvious public methods
- `//` inline only for non-obvious logic or business decisions — never to paraphrase code
- Prefix actionable notes with `TODO:`, `FIXME:`, or `HACK:`
- ❌ Never comment the obvious (`// Loop through items`)

---

## Project Structure

```
MyBon/
├── Areas/                    # Modular feature areas
│   ├── MainList/             # Main listing functionality
│   ├── MasterData/           # Master data management
│   ├── Outstanding/          # Outstanding items tracking
│   ├── QueueApproval/        # Approval queue management
│   └── StockManagement/      # Stock management features
├── Components/               # Reusable UI components
│   ├── QrTagging/            # QR code tagging component
│   └── SignatureLetter/      # Digital signature component
├── Controllers/              # MVC Controllers
├── Domain/                   # Business logic layer
│   ├── Abstractions/         # Reusable, project-agnostic interfaces (e.g. IContext, IIdentityContext)
│   ├── Constants/            # Application constants
│   ├── Entities/             # Domain entities
│   ├── Enums/                # Enumeration types
│   ├── Extensions/           # Domain extensions
│   ├── Interfaces/           # All project-specific interfaces
│   └── Services/             # Domain service implementations
├── Helpers/                  # Helper utilities
├── Infrastructure/           # Cross-cutting concerns
│   ├── Contexts/             # Request context implementations
│   ├── Jobs/                 # Scheduled jobs (Quartz)
│   ├── Middleware/           # Custom middleware
│   ├── Options/              # Configuration options
│   ├── Persistence/          # Database context & migrations
│   ├── Repositories/         # Repository implementations
│   ├── Services/             # Infrastructure service implementations (Email, Storage, etc.)
│   └── Templates/            # Email/Document templates
├── Models/                   # ViewModels, DTOs, and Requests grouped by module
│   └── [ModuleName]/
│       ├── ViewModels/       # e.g. Models/Incoming/ViewModels/IncomingViewModel.cs
│       ├── DTOs/             # e.g. Models/Incoming/DTOs/IncomingDto.cs
│       └── Requests/         # e.g. Models/Incoming/Requests/CreateIncomingRequest.cs
├── Native/                   # Native library dependencies
├── Views/                    # Razor views
├── wwwroot/                  # Static assets
│   ├── custom/
│   │   └── features/         # Feature-specific JS, grouped by area and module
│   │       ├── [area-name]/[module-name]/[action].js  # Controllers inside Areas/
│   │       └── [module-name]/[action].js              # Controllers outside Areas/
│   └── helpers/              # Reusable JS utilities used across features
├── Program.cs                # Application entry point
├── appsettings.json          # Configuration
└── Dockerfile                # Container configuration
```

---

## Architecture & Patterns

### Layer Structure

```
Presentation   → Controllers/, Views/, Models/ (ViewModels, DTOs & Requests)
Domain         → Entities/, Services/, Interfaces/, Abstractions/, Constants/, Enums/
Infrastructure → Persistence/, Repositories/, Services/, Middleware/, Jobs/, Contexts/
```

**Interface placement:**
- `Domain/Abstractions/` — reusable interfaces that are project-agnostic and portable across projects
- `Domain/Interfaces/` — all project-specific interfaces, regardless of where they are implemented
- `Domain/Services/` — service implementations that contain pure business logic
- `Infrastructure/Services/` — service implementations that depend on external systems, file system, email, HTTP clients, or other infrastructure concerns

### Entities

Entities inherit from `BaseEntity` and implement `IEntity`. Both are defined in `Domain/Abstractions/` — do not recreate them. Primary keys and navigation properties are declared directly on the entity.

**Rules:**
- Always inherit `BaseEntity` and implement `IEntity`
- Foreign key + navigation property declared together (`PelangganId` + `Pelanggan?`)
- Navigation properties are nullable (`Pelanggan?`)
- Required strings use `= null!`, not `= default!` or `= ""`
- No data annotations on entities — constraints belong in EF configuration

---

### Models (ViewModels, DTOs, Requests)

All models are plain classes (not records). Located in `Models/[ModuleName]/` grouped by type.

**DTO** — data transfer between layers, no UI concerns:
```csharp
// Models/KMLetter/DTOs/KMLetterAlertDto.cs
public class KMLetterAlertDto
{
    public int Id { get; set; }
    public string? AWB { get; set; }
    public double? DimensionDepth { get; set; }
    public double? DimensionWidth { get; set; }
    public double? DimensionHeight { get; set; }
    public double? DimensionTotal => DimensionDepth * DimensionWidth * DimensionHeight / 1_000_000; // computed props allowed
}
```

**ViewModel** — data passed to Razor views, may include UI permission flags:
```csharp
// Models/KMLetter/ViewModels/AlertViewModel.cs
public class AlertViewModel
{
    public int Id { get; set; }
    public List<AlertItemViewModel> Items { get; set; } = [];

    // Button Permissions
    public bool CanEdit { get; set; }
    public bool CanAdd { get; set; }
}
```

**Request** — form/AJAX input from the client, validated with data annotations:
```csharp
// Models/KMLetter/Requests/AlertInformationRequest.cs
public class AlertInformationRequest
{
    [Required]
    public int Id { get; set; }

    [MaxLength(100)]
    public string? Remark { get; set; }

    public List<AttachmentItemRequest> Attachments { get; set; } = [];
}
```

**Rules:**
- Use classes, not records
- Nullable properties use `?`, required strings use `null!`
- Collections initialize with `[]` (not `new List<T>()`)
- ViewModels may include `Can*` boolean flags for UI permission rendering
- Data annotations on Request classes only — not on DTOs or ViewModels

---

### Controller Pattern

`BaseController` is an abstract class defined in `Controllers/BaseController.cs` — do not recreate it. All controllers must inherit from it. It provides `ToActionResult()` and other shared utilities.

`IContext` is resolved lazily inside `BaseController` and exposed as the `Context` property — **do not inject `IContext` in controller constructors**:

```csharp
// DON'T
public class MyController(IContext context) : BaseController { }

// DO
public class MyController : BaseController
{
    public IActionResult Index()
    {
        var userId = Context?.Identity.Id;
    }
}
```

Services still inject `IContext` via constructor DI as normal — this exception applies to controllers only.

```csharp
[Area("AreaName")]
public class FeatureController(IFeatureService featureService) : BaseController
{
    private readonly IFeatureService _featureService = featureService;

    public async Task<IActionResult> Index()
    {
        var result = await _featureService.GetAllAsync();
        return ToActionResult(result, data => View(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequest request)
    {
        var result = await _featureService.CreateAsync(request);
        return ToActionResult(result);
    }
}
```

---

### Service Pattern

```csharp
// Domain/Interfaces/IFeatureService.cs
public interface IFeatureService
{
    Task<ErrorOr<Feature>> GetAllAsync();
    Task<ErrorOr<Feature>> GetByIdAsync(Guid id);
    Task<ErrorOr> CreateAsync(CreateRequest request);
    Task<BaseDatatableResponse> GetDatatableAsync(BaseDatatableRequest request);
}

// Domain/Services/FeatureService.cs  (or Infrastructure/Services/ if infra-dependent)
public class FeatureService(AppDbContext dbContext) : IFeatureService
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<ErrorOr<Feature>> GetByIdAsync(Guid id)
    {
        var item = await _dbContext.Features.FindAsync(id);
        if (item is null)
            return ErrorOr<Feature>.CreateFailure("Not found.", ErrorType.NotFound);

        return ErrorOr<Feature>.CreateSuccess("OK", item);
    }
}
```

> `BaseDatatableRequest` / `BaseDatatableResponse` are shared types in `Models/Common/` used for server-side DataTables integration.

---

### Error Handling

`ErrorOr` and `ErrorOr<T>` are **custom types** defined in `Models/Common/ErrorOr.cs` — do not install any NuGet package for this (e.g. do not install the `ErrorOr` package by Amantinband). Never throw exceptions for business errors.

**Shape:**
- `ErrorOr` — no data: `Message`, `Success`, `ErrorType`
- `ErrorOr<T>` — with data: inherits above + `Data`

**Factory methods — the only correct way to construct these:**
```csharp
// Without data
ErrorOr.CreateSuccess("Deleted successfully.");
ErrorOr.CreateFailure("Not found.", ErrorType.NotFound);

// With data
ErrorOr<T>.CreateSuccess("OK", item);
ErrorOr<T>.CreateFailure("Not found.", ErrorType.NotFound); // Data will be null
```

Do not use constructors directly — always use the static factory methods.

**Service — return `ErrorOr<T>` (with data) or `ErrorOr` (without data):**
```csharp
// With data
public async Task<ErrorOr<Incoming>> GetByIdAsync(Guid id)
{
    var item = await _dbContext.Incomings.FindAsync(id);
    if (item is null)
        return ErrorOr<Incoming>.CreateFailure("Data not found.", ErrorType.NotFound);

    return ErrorOr<Incoming>.CreateSuccess("OK", item);
}

// Without data (delete, void update, etc.)
public async Task<ErrorOr> DeleteAsync(Guid id)
{
    var item = await _dbContext.Incomings.FindAsync(id);
    if (item is null)
        return ErrorOr.CreateFailure("Data not found.", ErrorType.NotFound);

    _dbContext.Incomings.Remove(item);
    await _dbContext.SaveChangesAsync();
    return ErrorOr.CreateSuccess("Deleted successfully.");
}
```

**Controller — always use `ToActionResult()` from `BaseController`:**
```csharp
// Overload 1 — ErrorOr without data
var result = await _service.DeleteAsync(id);
return ToActionResult(result);

// Overload 2 — ErrorOr<T>, data returned directly to client
var result = await _service.GetByIdAsync(id);
return ToActionResult(result);

// Overload 3 — ErrorOr<T> with custom success handler (e.g. redirect to View)
var result = await _service.GetByIdAsync(id);
return ToActionResult(result, data => View(data));
```

`ToActionResult` automatically differentiates between AJAX and page requests:
- **AJAX** → JSON with `ErrorOr` object (`Message`, `Success`, `ErrorType`)
- **Page** → standard HTTP status codes

`ErrorType` → HTTP status mapping:

| ErrorType | HTTP |
|---|---|
| `NotFound` | 404 |
| `Forbidden` | 403 |
| `Validation` | 400 |
| `Conflict` | 409 |
| Others | 500 via `ServerError()` |

---

### Database Access

Prefer injecting `AppDbContext` directly into services via constructor injection.
Repositories exist in the project but **always confirm first** before creating or using one.

```csharp
// PREFER — inject AppDbContext directly
public class IncomingService(
    AppDbContext dbContext,
    ILogger<IncomingService> logger) : IIncomingService
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly ILogger<IncomingService> _logger = logger;
}
```

**Required:** always assign primary constructor parameters to `private readonly` fields — never access constructor parameters directly in method bodies.

```csharp
// DON'T
public async Task<ErrorOr<Incoming>> GetByIdAsync(Guid id)
{
    var item = await dbContext.Incomings.FindAsync(id); // ❌ direct parameter access
}

// DO
public async Task<ErrorOr<Incoming>> GetByIdAsync(Guid id)
{
    var item = await _dbContext.Incomings.FindAsync(id); // ✅ access via field
}
```

---

### Identity & User Context

Never access `HttpContext.User` directly in controllers or services.
Always use `IContext.Identity` set by `ContextMiddleware` per request.

```csharp
// DON'T
var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

// DO
var userId   = context.Identity.Id;
var username = context.Identity.Username;
var fullName = context.Identity.FullName;
var email    = context.Identity.Email;
```

`IContext` is injected via constructor DI in services. In controllers, access via the `Context` property from `BaseController` (do not inject in constructor). Available in Razor views via `@inject IContext Context`.

---

### File & Type Naming

| Type | Convention | Location |
|---|---|---|
| Controller | `[Name]Controller.cs` | `Controllers/` or `Areas/[Area]/Controllers/` |
| Service Interface | `I[Name]Service.cs` | `Domain/Interfaces/` |
| Domain Service Impl | `[Name]Service.cs` | `Domain/Services/` |
| Infra Service Impl | `[Name]Service.cs` | `Infrastructure/Services/` |
| Repository Interface | `I[Name]Repository.cs` | `Domain/Interfaces/` |
| Repository Impl | `[Name]Repository.cs` | `Infrastructure/Repositories/` |
| Entity | PascalCase | `Domain/Entities/` |
| DTO | `[Entity]Dto.cs` | `Models/[ModuleName]/DTOs/` |
| ViewModel | `[Entity]ViewModel.cs` | `Models/[ModuleName]/ViewModels/` |
| Request DTO | `[Action]Request.cs` | `Models/[ModuleName]/Requests/` |


---

### Frontend JS

Feature-specific JS lives in `wwwroot/custom/features/`, mirroring the controller location. Folder names use kebab-case.

| Controller location | JS path |
|---|---|
| `Areas/MainList/KMLetterController.cs` | `wwwroot/custom/features/main-list/kmletter/create.js` |
| `Controllers/IncomingController.cs` | `wwwroot/custom/features/incoming/create.js` |

**Rules:**
- Inside Areas → `custom/features/[area-name]/[module-name]/[action].js`
- Outside Areas → `custom/features/[module-name]/[action].js`
- Reusable utilities shared across features → `wwwroot/helpers/`


## Don'ts

- **Do not** throw exceptions for business errors — use `ErrorOr`
- **Do not** access `HttpContext.User` directly — use `IContext.Identity`
- **Do not** return `Ok()` / `NotFound()` manually — use `ToActionResult()`
- **Do not** access the file system directly — use `IStorageService`
- **Do not** send emails directly — use `IEmailService`
- **Do not** import new JS libraries without confirmation
- **Do not** create custom UI components if Metronic already provides one
- **Do not** create or use a repository without confirming first
- **Do not** access constructor parameters directly in method bodies — always assign to a `private readonly` field
- **Do not** inject `IContext` in controller constructors — use the `Context` property from `BaseController`
- **Do not** install any NuGet package for `ErrorOr` — it is a custom type in `Models/Common/`

## Auth Flow (External Service)

```
POST /auth/login (MyBon)
  → POST /api/auth/login (External Auth Service)
  ← { Id, Username, Email, FullName }
  → Map to Claims → Set Cookie (8 hours)
```

| Field | Claim Type |
|---|---|
| `Id` | `ClaimTypes.NameIdentifier` |
| `FullName` | `ClaimTypes.Name` |
| `Email` | `ClaimTypes.Email` |
| `Username` | `"usr"` |

Config in `appsettings.json`:
```json
{ "ExternalAuth": { "BaseUrl": "https://..." } }
```
