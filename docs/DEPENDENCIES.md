# SunyaSuite Dependencies

All package versions are managed centrally via `Directory.Packages.props`.

---

## NuGet Packages

### Data Access

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.2 | Infrastructure | PostgreSQL EF Core provider |
| `Microsoft.EntityFrameworkCore.Design` | 10.0.9 | Infrastructure, Web.Api | EF Core CLI tools (migrations) |
| `Microsoft.EntityFrameworkCore.Tools` | 10.0.9 | Infrastructure | EF Core Package Manager Console tools |
| `Microsoft.EntityFrameworkCore.InMemory` | 10.0.9 | (unused) | In-memory provider for testing |
| `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` | 10.0.9 | Web.Api | EF Core error pages for development |

### Identity & Authentication

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `Microsoft.Extensions.Identity.Stores` | 10.0.9 | Domain | `IdentityUser` base class for `ApplicationUser` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.9 | Infrastructure | ASP.NET Core Identity with EF Core storage |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.9 | Web.Api | JWT Bearer token authentication |
| `System.IdentityModel.Tokens.Jwt` | 8.19.1 | Web.Api | JWT token creation/validation |
| `Microsoft.IdentityModel.Tokens` | 8.19.1 | Web.Api | Token validation utilities |
| `Microsoft.AspNetCore.Components.WebAssembly.Authentication` | 10.0.9 | Web.Client | WASM auth abstractions |

### UI (Blazor Client)

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `MudBlazor` | 9.6.0 | Web.Client | Material Design component library |
| `Microsoft.AspNetCore.Components.WebAssembly` | 10.0.9 | Web.Client | Blazor WebAssembly runtime |
| `Microsoft.AspNetCore.Components.WebAssembly.DevServer` | 10.0.9 | Web.Client | Development server for WASM |
| `Microsoft.Extensions.Http` | 10.0.9 | Web.Client | `IHttpClientFactory` for typed HTTP clients |

### Validation

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `FluentValidation` | 11.11.0 | Application | Server-side request validation |
| `FluentValidation.DependencyInjectionExtensions` | 11.11.0 | Infrastructure | DI integration for validators |

### PDF Generation

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `QuestPDF` | 2026.6.1 | Infrastructure | PDF generation for invoices and money receipts (Community license) |

### Excel Export

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `ClosedXML` | 0.104.2 | Infrastructure | Excel file generation for client/project/invoice/report exports |

### Email

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `MailKit` | 4.17.0 | Infrastructure, Web.Api | SMTP email sending (Gmail) |

### Logging

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `Serilog.AspNetCore` | 10.0.0 | Infrastructure, Web.Api | Structured logging |
| `Serilog.Sinks.File` | 7.0.0 | (configurable) | File logging sink |
| `Serilog.Sinks.Console` | 6.0.0 | (configurable) | Console logging sink |

### Nepali Utilities

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `NepDate` | 2.0.7 | Infrastructure | Nepali (BS) date conversion |
| `NumericWordsConversion` | 2.1.1 | Infrastructure | Convert numbers to Nepali words |
| `QRCoder` | 1.6.0 | Infrastructure | QR code generation (packaged but **not currently used** in code) |

### API

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `Microsoft.AspNetCore.OpenApi` | 10.0.9 | Web.Api | OpenAPI/Swagger document generation |

### Health Checks

| Package | Version | Used In | Purpose |
|---------|---------|---------|---------|
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.9 | Infrastructure | EF Core health check for database connectivity |

### Testing (unused — no test projects)

| Package | Version | Purpose |
|---------|---------|---------|
| `xunit` | 2.9.3 | Test framework |
| `Microsoft.NET.Test.Sdk` | 17.13.0 | Test runner |
| `bunit` | 1.38.1 | Blazor component testing |
| `Moq` | 4.20.72 | Mocking |
| `FluentAssertions` | 7.2.0 | Fluent assertions |

---

## Legacy / Deprecated Packages

| Package | Status | Notes |
|---------|--------|-------|
| `QRCoder` | **Unused** | Listed in `Directory.Packages.props` but no `using` or code references found in any project. Likely planned for future use. |

---

## Key .NET SDK Configuration

| Setting | Value | Location |
|---------|-------|----------|
| Target framework | `net10.0` | `Directory.Build.props` |
| Nullable | `enable` | `Directory.Build.props` |
| ImplicitUsings | `enable` | `Directory.Build.props` |
| TreatWarningsAsErrors | `true` | `Directory.Build.props` |
| SDK version | `10.0.301` | `global.json` |
| Central package management | `true` | `Directory.Packages.props` |

---

## Docker

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Multi-container setup (API + WASM + PostgreSQL) |
| `docker-compose.override.yml` | Development overrides |
| `Dockerfile` | Container build for API |
| `.dockerignore` | Build context exclusions |
