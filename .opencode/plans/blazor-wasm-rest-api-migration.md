# Migration Plan: Blazor Server → REST API + Blazor WebAssembly

## Overview

**Architecture:**

```
┌─────────────────────┐     HTTP/JSON      ┌──────────────────┐
│  SunyaSuite.Web     │◄──────────────────►│  SunyaSuite.Web   │
│  (.NET Web API)     │     JWT Bearer     │  .Client          │
│                     │                    │  (Blazor WASM)    │
│  Controllers        │                    │                   │
│  JWT Auth           │                    │  MudBlazor UI     │
│  Swagger            │                    │  HTTP API calls   │
│  Services (unchanged)│                   │  DTO references   │
└─────────────────────┘                    └──────────────────┘
         │                                            │
         │                                    ┌───────┘
         ▼                                    ▼
  ┌─────────────────┐                ┌──────────────────┐
  │  SunyaSuite.MAUI │  (future)      │  Same API        │
  │  (.NET MAUI)     │◄──────────────►│  JWT Bearer      │
  └─────────────────┘                └──────────────────┘
```

**Key benefit:** MudBlazor stays. Your 89 Razor components migrate mostly as-is to the Client project. The main change is data access switches from `IDbContextFactory` to `HttpClient` API calls.

---

## What Stays vs Changes

| Aspect | Current (Blazor Server) | Target (API + WASM) |
|--------|------------------------|---------------------|
| **UI framework** | MudBlazor | MudBlazor (same!) |
| **Components** | `.razor` in `Components/` | `.razor` in `.Client` project |
| **Layout** | `MainLayout.razor` + `NavMenu.razor` | Same — moved to .Client |
| **Design theme** | `VercelTheme.cs` | `VercelTheme.cs` — ported to .Client |
| **Dark mode** | localStorage + JS interop | localStorage + JS interop (same) |
| **Data access** | `IDbContextFactory` (direct SQL) | `HttpClient` → API controllers |
| **Auth** | Cookie (Identity) + `SignInManager` | JWT Bearer + custom `AuthenticationStateProvider` |
| **DI services** | Injected from Infrastructure | HTTP calls to API |
| **PDF/Excel** | QuestPDF/ClosedXML (server) | Same — API returns `FileStreamResult` |
| **Passkey** | `PasskeySubmit.razor.js` | Same — works in browser WASM |
| **Charts** | MudBlazor MudChart | MudBlazor MudChart (same!) |
| **Icons** | `@Icons.Material.Filled.*` | Same — MudBlazor icons |

---

## Project Changes

### 1. `SunyaSuite.Web` → REST API

| Change | Details |
|--------|---------|
| Remove Blazor Server | `AddRazorComponents()`, `AddInteractiveServerComponents()`, `MapRazorComponents()`, circuits config |
| Remove UI files | All `Components/` directory, `Themes/`, `wwwroot/app.css`, `wwwroot/scripts.js` |
| Add API controllers | ~15 controllers wrapping existing service interfaces |
| Add JWT auth | `AddAuthentication().AddJwtBearer()` + token generation endpoint |
| Add Swagger | `AddOpenApi()` + `MapOpenApi()` |
| Add CORS | Allow WASM origin (e.g. `http://localhost:5002`) |
| Keep Identity | `AddIdentityCore<ApplicationUser>()` stays — used by AuthController for password verification |
| Keep Infrastructure | All services, `AddInfrastructure()`, hosted services, migrations, seed — unchanged |
| Keep middleware | Exception handler, HSTS, health checks, migration+seed |

**What `Program.cs` becomes:**

```csharp
var builder = WebApplication.CreateBuilder(args);

// Logging, Serilog (same)
Log.Logger = new LoggerConfiguration()...;
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5002")
              .AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    });
});

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.AdminOnly, p => p.RequireRole(RoleNames.Admin))
    .AddPolicy(PolicyNames.StaffOrAdmin, p => p.RequireRole(RoleNames.Admin, RoleNames.Staff));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddIdentityCore<ApplicationUser>(options => { ... })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddHostedService<OverdueBackgroundService>();
// EmailSettings, etc.

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
if (app.Environment.IsDevelopment()) app.MapOpenApi();

// Migrate + Seed (same)
using var scope = app.Services.CreateScope();
...
await app.RunAsync();
```

### 2. `SunyaSuite.Web.Client` → Blazor WASM

**New project** created via:
```bash
dotnet new blazorwasm -o src/SunyaSuite.Web.Client
```

**Project references:**
```
SunyaSuite.Web.Client
├── SunyaSuite.Application    (DTOs, interfaces — shared contracts)
└── SunyaSuite.Domain         (entities, enums)
```

**Packages:**
```xml
<PackageReference Include="MudBlazor" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Authentication" />
<PackageReference Include="Microsoft.Extensions.Http" />
```

**Structure:**
```
src/SunyaSuite.Web.Client/
├── Program.cs                    # WASM entry: AddHttpClient, AddMudServices, auth
├── _Imports.razor                # Global usings
├── App.razor                     # Router + MainLayout
├── wwwroot/
│   ├── appsettings.json          # API base URL
│   └── index.html                # HTML shell
├── Themes/
│   └── VercelTheme.cs            # Ported from Web project
├── Auth/
│   ├── JwtAuthenticationStateProvider.cs  # Custom provider reading JWT
│   ├── TokenManager.cs                    # localStorage read/write
│   └── LoginResponse.cs                   # Token response model
├── Layout/
│   ├── MainLayout.razor          # Sidebar + appbar (same as current)
│   ├── AuthLayout.razor          # Centered auth card
│   └── NavMenu.razor             # Sidebar nav (same as current)
├── Services/
│   ├── ClientsClient.cs          # HTTP client wrapping Clients API
│   ├── InvoicesClient.cs         # HTTP client wrapping Invoices API
│   ├── ProjectsClient.cs         # etc.
│   └── ...
├── Pages/
│   ├── Auth/
│   │   ├── Login.razor           # Calls AuthController.Login
│   │   ├── Register.razor        # Calls AuthController.Register
│   │   └── Logout.razor          # Clears token
│   ├── Clients/
│   │   ├── Index.razor           # Server-table → client-side with API data
│   │   ├── Create.razor          # Same MudBlazor form, POST via HttpClient
│   │   ├── Detail.razor          # Same detail view, data from API
│   │   └── Edit.razor            # Same edit form, PUT via HttpClient
│   ├── Invoices/
│   │   ├── Index.razor           # Same table, data from API
│   │   ├── Create.razor          # SAME MudBlazor form with dynamic rows
│   │   ├── Detail.razor          # Same detail view
│   │   └── Edit.razor            # Same edit form
│   ├── Projects/                 # Same structure
│   ├── Receipts/                 # Same structure
│   ├── Admin/                    # Users, Settings, FiscalYears, Trash, Audit
│   ├── Account/
│   │   └── Manage/               # 13 manage sub-pages
│   └── Dashboard/
│       └── Index.razor           # Same dashboard with MudChart
└── Shared/
    ├── PageHeader.razor          # Ported as-is
    ├── LoadingSkeleton.razor     # Ported as-is
    ├── StatusChip.razor          # Ported as-is
    └── ...                       # All 19 shared components
```

---

## Phase 1: REST API Backend (SunyaSuite.Web)

### 1.1 Update .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <!-- Remove MudBlazor -->
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\SunyaSuite.Infrastructure\SunyaSuite.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### 1.2 Rewrite Program.cs

Replace Blazor registrations with:
- `AddControllers()`
- `AddAuthentication().AddJwtBearer()`
- `AddCors()`
- `AddOpenApi()`
- `MapControllers()` instead of `MapRazorComponents()`

Keep:
- Identity + auth policies
- `AddInfrastructure()`
- `AddHostedService<OverdueBackgroundService>()`
- Exception handler + HSTS + health checks
- Migration + seed

Remove:
- All Blazor component/UI registrations
- `Components/` directory (~89 .razor files)
- `Themes/` directory
- `wwwroot/app.css`, `wwwroot/scripts.js`

### 1.3 Create API Controllers (~15 files)

| Controller | Endpoints | Service |
|-----------|-----------|---------|
| `AuthController` | `POST /api/auth/login`, `/register`, `/refresh`, `GET /api/auth/me` | Uses `UserManager` + JWT generation |
| `ClientsController` | `GET /api/clients`, `GET /api/clients/{id}`, `POST`, `PUT`, `DELETE`, `GET /api/clients/deleted`, `POST restore`, `DELETE permanent` | `IClientService` |
| `ProjectsController` | Same CRUD pattern | `IProjectService` |
| `InvoicesController` | CRUD + `PATCH /api/invoices/{id}/status` | `IInvoiceService` |
| `ReceiptsController` | CRUD | `IMoneyReceiptService` |
| `UsersController` | List, Get, Create, Update, Delete, Roles | `IUserService` |
| `DashboardController` | `GET /api/dashboard/stats`, `/recent` | `IDashboardService` |
| `ExportController` | `GET /api/export/clients`, `/projects`, `/invoices`, `/reports` | `IExportService` |
| `AuditLogController` | `GET /api/audit` | `IAuditService` |
| `FiscalYearsController` | CRUD + toggle | `IFiscalYearService` |
| `SettingsController` | `GET /api/settings`, `POST` | `IAppSettingService` |
| `BusinessProfileController` | `GET /api/business-profile`, `PUT`, logo upload | `IBusinessProfileService` |
| `NotificationsController` | `GET /api/notifications`, `POST toggle` | `INotificationPreferenceService` |
| `PdfController` | `GET /api/pdf/invoice/{id}`, `/receipt/{id}` | `IInvoicePdfService`, `IReceiptPdfService` |
| `PasskeyController` | `POST /api/passkey/request-options`, `/register`, `/authenticate` | Passkey logic |

### 1.4 Add JWT Configuration to appsettings.json

```json
{
  "Jwt": {
    "Key": "your-256-bit-secret-key-here",
    "Issuer": "SunyaSuite.Api",
    "Audience": "SunyaSuite.Web.Client",
    "ExpiryMinutes": 60,
    "RefreshTokenExpiryDays": 7
  },
  "Cors": {
    "Origins": "http://localhost:5002"
  }
}
```

---

## Phase 2: Blazor WASM Client (SunyaSuite.Web.Client)

### 2.1 Scaffold

```bash
dotnet new blazorwasm -o src/SunyaSuite.Web.Client
dotnet sln add src/SunyaSuite.Web.Client
```

### 2.2 Add references

```bash
# MudBlazor for UI (same components)
dotnet add src/SunyaSuite.Web.Client package MudBlazor
# Auth for WASM
dotnet add src/SunyaSuite.Web.Client package Microsoft.AspNetCore.Components.WebAssembly.Authentication
# HTTP client factory
dotnet add src/SunyaSuite.Web.Client package Microsoft.Extensions.Http
```

**Project references:**
```xml
<ProjectReference Include="..\SunyaSuite.Application\SunyaSuite.Application.csproj" />
<ProjectReference Include="..\SunyaSuite.Domain\SunyaSuite.Domain.csproj" />
```

### 2.3 Program.cs (WASM entry)

```csharp
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using SunyaSuite.Web.Client.Auth;
using SunyaSuite.Web.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");

// HTTP client for API calls
builder.Services.AddHttpClient("Api", client =>
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

// MudBlazor
builder.Services.AddMudServices();

// Auth
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());

// Client services (HTTP wrappers)
builder.Services.AddScoped<IClientService, ClientsClient>();
builder.Services.AddScoped<IInvoiceService, InvoicesClient>();
// ... etc. Each client wraps HttpClient calls to the API

await builder.Build().RunAsync();
```

### 2.4 Auth flow

**Login.razor:**
```csharp
// Login.razor code-behind
private async Task LoginUser()
{
    var response = await _http.PostAsJsonAsync("/api/auth/login", new
    {
        Email = Input.Email,
        Password = Input.Password,
        RememberMe = Input.RememberMe
    });

    if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        await _tokenManager.SetTokenAsync(result!.Token);
        _navigation.NavigateTo("/");
    }
    else
    {
        errorMessage = "Invalid login attempt.";
    }
}
```

**JwtAuthenticationStateProvider:**
```csharp
public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly TokenManager _tokenManager;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenManager.GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = ParseToken(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
```

**AuthorizeView — works identically to Blazor Server:**
```razor
<AuthorizeView Policy="@PolicyNames.AdminOnly">
    <Authorized>
        <a href="/users">Users</a>
    </Authorized>
</AuthorizeView>
```

### 2.5 Data access pattern

**Blazor Server (current):**
```csharp
@inject IClientService ClientService
// ...
var clients = await ClientService.GetPagedAsync(page, pageSize, ...);
```

**Blazor WASM (target):**
```csharp
@inject IClientService ClientService
// ...
var clients = await ClientService.GetPagedAsync(page, pageSize, ...);
```

The `IClientService` interface stays the same, but the implementation changes:
- **Server side:** `ClientService` (in Infrastructure) — uses `IDbContextFactory`
- **WASM side:** `ClientsClient` (in .Client) — uses `HttpClient` to call API

Both implement `IClientService`, so the Razor components don't change!

**ClientsClient.cs:**
```csharp
public class ClientsClient : IClientService
{
    private readonly HttpClient _http;

    public ClientsClient(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("Api");
    }

    public async Task<PagedResult<ClientListItemDto>> GetPagedAsync(
        int page, int pageSize, string? sortLabel, string? sortDirection,
        string? searchTerm, ClientFilterDto? filter, CancellationToken ct)
    {
        var query = new Dictionary<string, object?>
        {
            ["page"] = page, ["pageSize"] = pageSize,
            ["sortLabel"] = sortLabel, ["sortDirection"] = sortDirection,
            ["searchTerm"] = searchTerm
        };
        var response = await _http.GetAsync($"/api/clients?{QueryString(query)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<ClientListItemDto>>(ct) ?? new();
    }
    // ... other methods call respective API endpoints
}
```

**Benefit:** Existing Razor components need minimal changes — just the DI registration changes. The interface is identical.

### 2.6 Pages that need data refetching instead of direct DB access

| Blazor Server Page | WASM Page | Change |
|-------------------|-----------|--------|
| `Clients/Index.razor` | Same | `@inject IClientService` → same interface, different implementation |
| `Invoices/Create.razor` | Same | Dynamic rows + real-time totals work identically — MudBlazor is client-side |
| `Dashboard/Index.razor` | Same | `@inject IDashboardService` → same interface |
| `Account/Manage/*` | Same | Auth info from JWT claims instead of `SignInManager` |
| `Login.razor` | Rewritten | POST to API instead of `SignInManager.PasswordSignInAsync()` |

**Key insight:** Because MudBlazor runs entirely client-side, complex forms (Invoice Create with dynamic rows, real-time totals) work **identically** in WASM — all the UI logic is already running in the browser via the .NET WASM runtime. The only difference is where the data comes from.

### 2.7 MudBlazor components that stay the same

| Component | Status | Notes |
|-----------|--------|-------|
| `<MudTable>` with `ServerData` | ✅ Same | Replace with client-side data from API instead of `IDbContextFactory` |
| `<MudForm>` + validation | ✅ Same | Works identically in WASM |
| `<MudDatePicker>` | ✅ Same | Same |
| `<MudSelect>`, `<MudTextField>` | ✅ Same | Same |
| `<MudButton>`, `<MudIconButton>` | ✅ Same | Same |
| `<MudChart>` (Dashboard) | ✅ Same | Same |
| `<MudDialog>` | ✅ Same | Same |
| `<MudProgressLinear>` | ✅ Same | Same |
| `<MudSnackbar>` (notifications) | ✅ Same | Same |
| MudBlazor Icons (`@Icons.Material.*`) | ✅ Same | Same |
| `<MudThemeProvider>` with VercelTheme | ✅ Same | Port `VercelTheme.cs` to .Client project |
| Dark mode toggle | ✅ Same | JS interop with localStorage works in WASM too |

---

## Phase 3: Shared Infrastructure

### 3.1 API Client Service layer

Create one HTTP client class per service interface in the .Client project. Each class implements the same interface used by the Razor components.

| Interface | WASM Client | API Endpoints |
|-----------|-------------|---------------|
| `IClientService` | `ClientsClient.cs` | `GET/POST/PUT/DELETE /api/clients/*` |
| `IInvoiceService` | `InvoicesClient.cs` | `GET/POST/PUT/PATCH/DELETE /api/invoices/*` |
| `IProjectService` | `ProjectsClient.cs` | `GET/POST/PUT/DELETE /api/projects/*` |
| `IMoneyReceiptService` | `ReceiptsClient.cs` | `GET/POST/DELETE /api/receipts/*` |
| `IUserService` | `UsersClient.cs` | `GET/POST/PUT/DELETE /api/users/*` |
| `IDashboardService` | `DashboardClient.cs` | `GET /api/dashboard/*` |
| `IExportService` | `ExportClient.cs` | `GET /api/export/*` (returns blob) |
| `IAuditService` | `AuditClient.cs` | `GET /api/audit` |
| `IFiscalYearService` | `FiscalYearClient.cs` | `GET/POST/PATCH /api/fiscal-years/*` |
| `IAppSettingService` | `SettingsClient.cs` | `GET/POST /api/settings` |
| `IBusinessProfileService` | `BusinessProfileClient.cs` | `GET/PUT /api/business-profile` |
| `INotificationPreferenceService` | `NotificationsClient.cs` | `GET/POST /api/notifications` |

### 3.2 DI registration in .Client/Program.cs

```csharp
builder.Services.AddScoped<IClientService, ClientsClient>();
builder.Services.AddScoped<IInvoiceService, InvoicesClient>();
// ... etc.
```

This means all existing Razor components inject interfaces and work without changes — just the implementation changes from server-side to HTTP-based.

### 3.3 MudBlazor VercelTheme port

Copy `Themes/VercelTheme.cs` from Web project to .Client project. It's pure C# code with no server dependencies — works identically in WASM.

---

## Phase 4: File Changes Summary

### Files to ADD

| Location | Files | Count |
|----------|-------|-------|
| `SunyaSuite.Web/Controllers/` | API controllers | ~15 |
| `SunyaSuite.Web/Program.cs` rewrite | 1 | 0 |
| `SunyaSuite.Web.Client/` | Entire project scaffold | ~10 |
| `SunyaSuite.Web.Client/Pages/` | Razor pages (ported) | ~45 |
| `SunyaSuite.Web.Client/Shared/` | Shared components (ported) | ~19 |
| `SunyaSuite.Web.Client/Services/` | HTTP client classes | ~12 |
| `SunyaSuite.Web.Client/Auth/` | JWT auth provider | ~3 |
| `SunyaSuite.Web.Client/Layout/` | Layout + nav | ~4 |
| `SunyaSuite.Web.Client/Themes/` | VercelTheme.cs | ~1 |
| **Total new** | | **~110** |

### Files to REMOVE

| Location | Files | Count |
|----------|-------|-------|
| `Components/` (all `.razor`) | Move to .Client | ~89 |
| `Components/` C# UI infra | Auth providers, redirect manager, etc. | ~6 |
| `Themes/VercelTheme.cs` | Move to .Client | ~1 |
| `wwwroot/app.css`, `scripts.js` | Server no longer serves UI | ~2 |
| **Total removed** | | **~98** |

### Files to MODIFY

| File | Change |
|------|--------|
| `SunyaSuite.Web/Program.cs` | Blazor → API + JWT + CORS |
| `SunyaSuite.Web.csproj` | Remove MudBlazor, add JWT + OpenApi |
| `SunyaSuite.slnx` | Add .Client project |
| `Directory.Packages.props` | Add JWT package, keep MudBlazor |

---

## Phase 5: Migration Order

| Step | Task | Files |
|------|------|-------|
| 1 | Add JWT packages to `Directory.Packages.props` + Web .csproj | 2 |
| 2 | Scaffold `SunyaSuite.Web.Client` project | ~10 |
| 3 | Add .Client to solution + add references (Application, Domain) | 2 |
| 4 | Port `VercelTheme.cs` to .Client | 1 |
| 5 | Port Layout + NavMenu to .Client | 4 |
| 6 | Port Shared components to .Client (19 files) | 19 |
| 7 | Create `AuthController` in Web + JWT generation | 1 |
| 8 | Create `JwtAuthenticationStateProvider` + `TokenManager` in .Client | 3 |
| 9 | Create Auth pages (Login, Register, Logout) in .Client | 3 |
| 10 | Create API controllers in Web (~15 files) | 15 |
| 11 | Create HTTP client services in .Client (~12 files) | 12 |
| 12 | Rewrite `Program.cs` in Web (Blazor → API) | 1 |
| 13 | Port Dashboard page to .Client | 1 |
| 14 | Port Clients pages to .Client (Index, Create, Detail, Edit) | 4 |
| 15 | Port Projects pages to .Client | 4 |
| 16 | Port Invoices pages to .Client (most complex — dynamic rows) | 4 |
| 17 | Port Receipts pages to .Client | 3 |
| 18 | Port Admin pages to .Client (Users, Settings, Trash, Audit, FiscalYears) | 6 |
| 19 | Port Account/Manage pages to .Client (13 sub-pages) | 13 |
| 20 | Remove Blazor files from Web project (Components/, Themes/, wwwroot) | ~98 |
| 21 | Build + test | — |

---

## Summary

| Metric | Value |
|--------|-------|
| **New API controllers** | ~15 |
| **New .Client project files** | ~110 (pages, services, auth, layout) |
| **Files removed from Web** | ~98 (Razor components, themes, CSS) |
| **Files modified** | ~5 (.csproj, Program.cs, solution, packages) |
| **MudBlazor components kept** | All (Dashboard charts, tables, forms, dialogs, icons) |
| **Razor component changes** | Minimal — same `@inject IInterface`, different implementation |
| **Auth model** | Cookie → JWT |
| **Future MAUI** | Same API, same JWT auth — just new MAUI client project |
