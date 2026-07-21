# SunyaSuite — Agent Guide

This file is read automatically by agentic coding tools. It links to the full documentation set and gives concrete guidance for adding features.

---

## Quick Start

```powershell
# Run both API + WASM client
.\run.ps1

# Or run individually
dotnet run --project src/SunyaSuite.Web --launch-profile http
dotnet run --project src/SunyaSuite.Web.Client --launch-profile http

# Build
dotnet build

# Publish
dotnet publish src/SunyaSuite.Web
```

- **API**: `http://localhost:5000`
- **Client**: `http://localhost:5002`
- **Swagger**: `http://localhost:5000/openapi/v1.json`

---

## Documentation Index

| Doc | When to read |
|-----|-------------|
| [ARCHITECTURE.md](ARCHITECTURE.md) | Before adding a new project/layer or changing the solution structure |
| [CONVENTIONS.md](CONVENTIONS.md) | Before writing any C#, Razor, or DI registration — naming, patterns, pitfalls |
| [DOMAIN.md](DOMAIN.md) | Before modifying entities or business rules — glossary, logic locations, status transitions |
| [DATA-MODEL.md](DATA-MODEL.md) | Before adding a new entity, table, migration, or changing EF Core mappings |
| [API-CONTRACTS.md](API-CONTRACTS.md) | Before adding/modifying a controller endpoint — auth, DTOs, routes |
| [TESTING.md](TESTING.md) | Before writing tests — strategy, mocking conventions, coverage targets |
| [DEPENDENCIES.md](DEPENDENCIES.md) | Before adding a NuGet package — check what's already available |

---

## Adding a New Feature — 5 Dos and Don'ts

### ✅ DO
1. **Follow the existing folder pattern**: New entity → `Domain/Entities/Tenant/`, DTO + interface → `Application/DTOs/Tenant/` + `Application/Interfaces/Tenant/`, service impl → `Infrastructure/Services/Tenant/`, controller → `Web/Controllers/Tenant/`, wizard → `Web.Client/Pages/{Feature}/`
2. **Register via DbContextFactory pattern**: Use `AddDbContextFactory<T>()` + scoped factories, never inject `DbContext` directly
3. **Implement `ICompanyScoped`** for any entity that belongs to a company — the `ForCompany<T>()` extension makes scoping automatic
4. **Use `record` for response DTOs, `class` for request DTOs** that need validation attributes or computed properties
5. **Wire DI in `Infrastructure/DependencyInjection.cs`** — don't scatter registrations across projects

### ❌ DON'T
1. **Don't use `MediatR` or `AutoMapper`** — this codebase uses direct service injection and manual mapping
2. **Don't bypass FluentValidation** — all Create/Update requests must have a validator in `Application/Validators/`
3. **Don't add `LoadingSkeleton` to Create pages** that don't load async data — only Edit and async-Create pages need it
4. **Don't mix `Snackbar` and `_errorMessage`** for the same error type — load errors go to `_errorMessage`, save/action errors to `Snackbar`
5. **Don't forget the soft-delete pattern** — new tenant entities should have `IsDeleted` + `DeletedAt` + query filter + service methods

---

## Areas Never to Modify Without Explicit Approval

- `Directory.Build.props` and `Directory.Packages.props` — central build/package configuration
- `global.json` — .NET SDK version pinning
- Existing migration files in `Infrastructure/Data/{Config,Tenant}/Migrations/` — always add new migrations
- Docker deployment files (`docker-compose.yml`, `Dockerfile`) — requires infrastructure review
- `AGENTS.md` — project-specific agent skill configuration

---

## Understanding the Multi-Tenant Architecture

```
X-Tenant-ID header
  → TenantMiddleware (resolves org slug → org ID)
  → TenantContext (ambient scoped context)
  → TenantDbContextFactory (creates tenant-specific DbContext)
  → ICompanyScoped + ForCompany<T>() (filters queries)
```

New tenant-scoped features always follow this pipeline.
