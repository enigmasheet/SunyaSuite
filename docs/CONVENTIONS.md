# SunyaSuite Conventions

This document captures actual patterns observed across the codebase. New code should follow the **new-code conventions** column where legacy and new diverge.

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Namespaces | `SunyaSuite.{Layer}.{Area}` | `SunyaSuite.Application.DTOs.Tenant` |
| Entities | PascalCase singular | `Invoice`, `Client`, `FiscalYear` |
| DTOs | PascalCase with suffix | `ClientListItemDto`, `CreateInvoiceRequest` |
| Interfaces | `I{PascalCase}` | `IClientService`, `ICompanyScoped` |
| Private fields | `_camelCase` | `_errorMessage`, `_loading`, `_form` |
| Parameters | `camelCase` | `ct`, `searchTerm`, `request` |
| Methods | PascalCase | `CreateAsync`, `Recalculate`, `GetPagedAsync` |
| Async suffix | `Async` suffix on Task-returning methods | `SaveAsync()`, `GetByIdAsync()` |
| Constants | PascalCase | `SystemAdmin`, `OrgRole` |
| Enums | PascalCase | `BillType.VatBill`, `InvoiceStatus.Draft` |
| Enum members | PascalCase | `VatBill`, `Green`, `Sent` |
| Settings classes | PascalCase with `Settings` suffix | `VatSettings`, `EmailSettings` |
| Section name const | `SectionName` property | `public const string SectionName = "Vat";` |

---

## Project Structure Conventions

### Application Layer
- **DTOs** grouped by area: `DTOs/Config/`, `DTOs/Tenant/`
- **Interfaces** mirror DTO grouping: `Interfaces/Config/`, `Interfaces/Tenant/`
- **Validators** in flat `Validators/` folder
- **Settings** in flat `Settings/` folder

### Infrastructure Layer
- **Services** grouped: `Services/Config/`, `Services/Tenant/`, `Services/Admin/`
- **Data contexts** grouped: `Data/Config/`, `Data/Tenant/`
- **Entity configurations** in `Configurations/` subfolder next to their DbContext
- **Migrations** in `Migrations/` subfolder (with `ConfigDb/` subfolder for Config DB)

### Web.Api Layer
- **Controllers** grouped: `Controllers/Config/`, `Controllers/Tenant/`
- **Extensions** in flat `Extensions/` folder
- **Auth-related** in `Auth/` folder

### Web.Client Layer
- **Pages** grouped by feature: `Pages/Clients/`, `Pages/Invoices/`, `Pages/Admin/`
- **Shared components** in flat `Shared/` folder
- **Layouts** in `Layout/` folder
- **HTTP service clients** in flat `Services/` folder

---

## Dependency Injection

- **DbContextFactory pattern** used (not direct DbContext injection) — `AddDbContextFactory<T>()` for both `ConfigDbContext` and `ApplicationDbContext`
- **Tenant-specific scoping** via `TenantDbContextFactory` implementing `IDbContextFactory<ApplicationDbContext>`
- All service implementations are registered as **Scoped** in `DependencyInjection.cs`
- Singleton registrations: `TimeProvider.System`, `TokenManager`, `OrgManager`, `JwtAuthenticationStateProvider`
- No MediatR — services are injected directly as interfaces
- DI registration lives in `Infrastructure/DependencyInjection.cs` (called from `Web.Api/Program.cs`)

---

## Async Conventions

- All I/O methods return `Task` or `Task<T>`
- Cancellation tokens (`CancellationToken ct = default`) passed through all service methods and DbContext calls
- `ConfigureAwait(false)` used only in top-level catch blocks (not in library code)
- `ValueTask` not used

---

## Error Handling Patterns

### API Controllers
```csharp
// Success: return Ok(result)
// Not found: return NotFound()
// Bad request: return BadRequest(new { message = "..." })
// Conflict: return Conflict(new { message = "..." })
// No content: return NoContent()
```

### Blazor Components
- **Load errors**: `_errorMessage` field → `<ErrorAlert Message="@_errorMessage" />`
- **Save errors**: `Snackbar.Add(...)` for non-blocking toast
- **Try-catch in all async handlers** — never let exceptions propagate to the Blazor renderer
- `_loading`/`_errorMessage` guard pattern:
  ```csharp
  try { ... }
  catch { _errorMessage = "Failed to load."; }
  finally { _loading = false; }
  ```

### Common Pitfalls (from AGENTS.md)
- ❌ Do NOT use `Visible` parameter on `ErrorAlert` — it doesn't exist
- ❌ Do NOT use `Message="_errorMessage"` without `@` prefix — must be `Message="@_errorMessage"`
- ❌ Do NOT mix `_errorMessage` for save AND load errors — use `Snackbar` for saves
- ❌ Do NOT add `LoadingSkeleton` to Create pages without async data load
- ✅ Reset `_errorMessage` in the try block before calling the service, not after

---

## DTO Conventions

- **Prefer `record` types** for DTOs (immutable, value equality)
- **Use `class`** when computed properties are needed (e.g., `InvoiceItemDto.Amount`)
- Request DTOs can be `class` for DataAnnotations compatibility
- Paged results use generic `PagedResult<T>` record
- DTOs live in the Application layer, never in Domain

---

## Soft-Delete Pattern

```csharp
public class Client : ICompanyScoped
{
    // ...
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

- Consistent across: `Client`, `Invoice`, `Project`, `MoneyReceipt`, `Company`, `Branch`, `Organization`
- EF Core query filter: `HasQueryFilter(x => !x.IsDeleted)`
- Service methods follow: `DeleteAsync` (soft) → `RestoreAsync` → `PermanentDeleteAsync`
- Soft-deleted items listed via `GetDeletedPagedAsync`

---

## Concurrency

- `Invoice` and `MoneyReceipt` use `byte[] RowVersion` with `IsConcurrencyToken()` for optimistic concurrency
- Other entities do not use concurrency tokens

---

## Nullable Reference Types

- Enabled globally via `Directory.Build.props`: `<Nullable>enable</Nullable>`
- All entity reference navigation properties use `= null!;` for required relationships
- Optional navigations use `?` nullable annotation (e.g., `Branch? BranchInfo`)
- `TreatWarningsAsErrors` is on — nullable warnings are compile errors

---

## Logging

- **Serilog** configured in `Program.cs` with console sink
- Bootstrap logger (`Log.Logger`) created before `WebApplication.CreateBuilder`
- `Log.Fatal` used for startup failures (DB migration, seeding)
- Infrastructure services use `ILogger<T>` constructor injection
- Application layer has **no logging** — it's infrastructure concern

---

## Record vs Class Decision Guide

| Use `record` | Use `class` |
|-------------|-------------|
| Immutable DTOs | DTOs with computed properties |
| Response types | Request types that need DataAnnotations |
| Simple data carriers | Types with logic (validators, settings) |
| `PagedResult<T>` | `InvoiceItemDto` (computed `Amount`) |

---

## Unit of Work

- No explicit Unit of Work pattern — EF Core's `DbContext.SaveChangesAsync()` serves this role
- `IDbContextFactory<T>` creates short-lived contexts per operation
- No `ITransaction` abstraction — raw `context.Database.BeginTransaction()` used in services that span multiple operations (e.g., `InvoiceService.CreateAsync`)
