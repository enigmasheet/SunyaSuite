# SunyaSuite Architecture

## Overview

SunyaSuite is a .NET 10 multi-tenant business management platform (invoicing, project management, client management, money receipts) built using Clean Architecture. The frontend is a Blazor WebAssembly standalone app communicating with a REST API backend.

---

## Solution Structure (5 projects)

```
SunyaSuite.slnx
├── src/
│   ├── SunyaSuite.Domain           (classlib)
│   ├── SunyaSuite.Application      (classlib)
│   ├── SunyaSuite.Infrastructure   (classlib)
│   ├── SunyaSuite.Web.Api          (webapi)
│   └── SunyaSuite.Web.Client       (blazor wasm)
├── docs/
├── docker-compose.yml
├── Dockerfile
├── Directory.Build.props
├── Directory.Packages.props
└── global.json
```

### Project Reference Graph

```
SunyaSuite.Web.Client ──────────┬── SunyaSuite.Application
                                └── SunyaSuite.Domain

SunyaSuite.Web.Api ─────── SunyaSuite.Infrastructure ─────── SunyaSuite.Application ─────── SunyaSuite.Domain
```

### Layer Responsibilities

| Layer | Path | Role |
|-------|------|------|
| **Domain** | `src/SunyaSuite.Domain/` | Entities, enums, constants, marker interfaces (`ICompanyScoped`). Zero dependencies. |
| **Application** | `src/SunyaSuite.Application/` | DTOs, service interfaces, FluentValidation validators, settings POCOs. Depends only on Domain. |
| **Infrastructure** | `src/SunyaSuite.Infrastructure/` | EF Core DbContexts + configurations, migrations, all service implementations, PDF generation, email, background jobs. Depends on Application. |
| **Web.Api** | `src/SunyaSuite.Web/` | REST API controllers, JWT auth, middleware (tenant resolution, CORS), startup wiring. Depends on Infrastructure. |
| **Web.Client** | `src/SunyaSuite.Web.Client/` | Blazor WASM pages, MudBlazor UI components, HTTP service clients, client-side auth. Depends on Application + Domain. |

---

## Multi-Tenant Database Architecture

SunyaSuite uses a **hybrid multi-tenant** approach with two database levels:

### Config Database (shared, single instance)
- **Connection**: `ConfigConnection` in `appsettings.json`
- **Purpose**: Stores organizations, ASP.NET Core Identity users, roles, organization memberships, invites
- **DbContext**: `ConfigDbContext` — inherits from `IdentityDbContext<ApplicationUser>`
- **Always shared** across all tenants

### Tenant Database(s)
- **Connection**: `TemplateConnection` in `appsettings.json` (base connection)
- **Purpose**: Stores all business data — companies, branches, clients, invoices, projects, money receipts, fiscal years, audit logs
- **DbContext**: `ApplicationDbContext` — inherits from plain `DbContext`
- **Per-organization**: Organizations with `ConnectionString` set get their **own separate database**; otherwise all share the template database
- **Migration**: `ApplyTenantMigrationsService` (hosted service) discovers all orgs at startup and applies migrations to each distinct connection string

### Database Flow

```
Request → X-Tenant-ID header
         → TenantMiddleware resolves org slug
         → TenantContext holds org ID, slug, connection string
         → TenantDbContextFactory creates ApplicationDbContext
           with either template or org-specific connection string
         → Company-scoped queries via ICompanyScoped + ForCompany<T>()
```

---

## Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **CQRS** | Not used | Traditional service-layer pattern with interfaces |
| **ORM** | EF Core 10 + PostgreSQL | Npgsql provider |
| **Mapping** | Manual | No AutoMapper; DTOs mapped manually in service implementations |
| **Validation** | FluentValidation | Server-side validation in Application layer |
| **Auth** | JWT Bearer + ASP.NET Core Identity | Symmetric key, custom org-role authorization handler |
| **PDF** | QuestPDF | Community license, generates invoices + receipts |
| **Excel** | ClosedXML | Client/project/invoice/report exports |
| **Email** | MailKit | SMTP via Gmail, identity email sender + custom notification |
| **Logging** | Serilog | Console sink, file sink configuration available |

---

## Identity & Authorization Model

```
ApplicationUser (extends IdentityUser)
  ├── FirstName, LastName, CreatedAt, Preference
  └── OrganizationUsers (join table)
        ├── OrganizationId → Organization
        ├── Role (Owner | OrgAdmin | Member | Viewer)
        └── DefaultCompanyId, DefaultBranchId
```

### Authorization Policies

| Policy | Effective Roles |
|--------|----------------|
| `SystemAdminOnly` | SystemAdmin |
| `OrgAdminOrAbove` | Owner, OrgAdmin |
| `OrgMemberOrAbove` | Owner, OrgAdmin, Member |
| `OrgViewerOrAbove` | Owner, OrgAdmin, Member, Viewer |

---

## Middleware Pipeline (Web.Api)

```
UseStatusCodePages
UseCors("AllowClient")
UseAuthentication
UseMiddleware<TenantMiddleware>    ← resolves X-Tenant-ID → ITenantContext
UseAuthorization
MapControllers
```

---

## Startup Sequence

1. Serilog bootstrap logger
2. QuestPDF Community license
3. `AddControllers()` + `AddOpenApi()`
4. `AddCorsPolicy()` — allow Blazor client origin(s)
5. `AddJwtAuthentication()` — JWT Bearer with symmetric key
6. `AddInfrastructure()` — EF Core DbContext factories, DI for all services
7. `AddIdentityServices()` — ASP.NET Core Identity with ConfigDbContext
8. `AddAppAuthorization()` — policy-based auth with OrgRoleAuthorizationHandler
9. `AddAppServices()` — JwtTokenService, MailKitEmailSender, settings, OverdueBackgroundService
10. `ConfigurePipeline()` — middleware ordering
11. `RunDatabaseStartupAsync()` — run migrations + seed data

---

## Legacy vs Active Development

**All code is actively developed on .NET 10.** There are no legacy .NET Framework projects in this solution. The entire codebase targets `net10.0` with `Nullable` enabled, `ImplicitUsings`, and `TreatWarningsAsErrors`.

---

## Folder Convention Within Projects

- **Domain**: `Entities/Config/`, `Entities/Tenant/`, `Enums/`, `Constants/`, `Interfaces/`
- **Application**: `DTOs/Config/`, `DTOs/Tenant/`, `Interfaces/Config/`, `Interfaces/Tenant/`, `Validators/`, `Settings/`
- **Infrastructure**: `Data/Config/`, `Data/Tenant/`, `Services/Config/`, `Services/Tenant/`, `Services/Admin/`, `DataSeeding/`, `HealthChecks/`, `EmailTemplates/`
- **Web.Api**: `Controllers/Config/`, `Controllers/Tenant/`, `Extensions/`, `Middleware/`, `Auth/`, `Services/Config/`
- **Web.Client**: `Pages/` (by feature), `Layout/`, `Shared/`, `Auth/`, `Services/`, `Themes/`, `Extensions/`
