# Migration Plan: Blazor Server → React + TypeScript + .NET API

## Overview

**Goal:** Remove all Blazor UI (89 `.razor` files, 4 shared `.cs` files), convert the .NET backend into a REST API, and build a new React + TypeScript frontend.

**Current stack:** Blazor Server (InteractiveServer), MudBlazor, cookie-based Identity, `IDbContextFactory` data access  
**Target stack:** React + TypeScript + Vite + Tailwind CSS + shadcn/ui, .NET 10 Web API with JWT auth, `IDbContextFactory` stays untouched

**Projects affected:**
| Project | Action |
|---------|--------|
| `SunyaSuite.Web` | Convert Blazor host → API host (~95 files removed, ~18 controllers added) |
| `SunyaSuite.Web.Client` | **New** — React + TypeScript frontend (~100+ new files) |
| `SunyaSuite.Infrastructure` | Untouched (services, DbContext, migrations stay) |
| `SunyaSuite.Application` | Untouched (DTOs, interfaces, validators stay) |
| `SunyaSuite.Domain` | Untouched (entities, enums stay) |

---

## Phase 1: Convert Web Project to API Backend

### 1.1 Remove Blazor-specific code from Program.cs

Remove these lines (exact locations from current `Program.cs`):

| Lines | Registration | Replacement |
|-------|-------------|-------------|
| 28-29 | `builder.Services.AddRazorComponents().AddInteractiveServerComponents()` | `builder.Services.AddControllers()` |
| 31 | `builder.Services.AddMudServices()` | Remove (frontend handles UI) |
| 33-35 | `AddCascadingAuthenticationState()`, `IdentityRedirectManager`, custom `AuthenticationStateProvider` | Remove (JWT-based in React) |
| 53-59 | `CircuitOptions` configuration | Remove (Blazor Server circuit, irrelevant) |
| 99 | `app.UseAntiforgery()` | Remove (not needed for API) |
| 102 | `app.MapStaticAssets()` | Keep if hosting SPA, or serve via separate process |
| 104-105 | `app.MapRazorComponents<App>().AddInteractiveServerRenderMode()` | `app.MapControllers()` |
| 107 | `app.MapAdditionalIdentityEndpoints()` | Remove (replaced by `AuthController`) |

### 1.2 Add API-specific registrations

Add to `Program.cs` in order:

```csharp
// After AddRazorComponents removal — replace with:
builder.Services.AddControllers();

// JWT Authentication (replaces cookie auth)
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

// CORS — allow React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Swagger/OpenAPI
builder.Services.AddOpenApi();
```

Update middleware pipeline:

```csharp
var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
```

### 1.3 Remove files (95 total)

| Directory | Files to delete | Count |
|-----------|----------------|-------|
| `Components/Pages/` | All `.razor` files (Clients, Invoices, Projects, Receipts, Users, Admin, AuditLog, Settings, Home, Error, NotFound, Trash, Reports, Account/*) | ~55 |
| `Components/Layout/` | `MainLayout.razor`, `AuthLayout.razor`, `NavMenu.razor`, `ReconnectModal.razor` + `.css` + `.js` | ~6 |
| `Components/Shared/` | `PageHeader.razor`, `LoadingSkeleton.razor`, `EmptyState.razor`, `ErrorAlert.razor`, `FormCard.razor`, `FormActions.razor`, `IndexToolbar.razor`, `SearchField.razor`, `ActionButtons.razor`, `ConfirmDeleteDialog.razor`, `PromptDialog.razor`, `FilterPanel.razor`, `StatusFilterGroup.razor`, `DateRangeFilter.razor`, `DetailInfoCard.razor`, `StatusChip.razor` | ~16 |
| `Components/Account/` | All auth pages, manage pages, shared account components (`.razor` + `.razor.js`) | ~27 |
| `Components/` | `App.razor`, `Routes.razor`, `_Imports.razor` | ~3 |
| `Themes/` | `VercelTheme.cs` | ~1 |
| `wwwroot/` | `app.css`, `scripts.js`, `favicon.png` | ~3 |
| Account C# infra | `IdentityRevalidatingAuthenticationStateProvider.cs`, `IdentityRedirectManager.cs`, `IdentityNoOpEmailSender.cs`, `IdentityComponentsEndpointRouteBuilderExtensions.cs`, `PasskeyInputModel.cs`, `PasskeyOperation.cs` | ~6 |

### 1.4 Add API controllers (18 controllers)

Each controller wraps an existing service interface from `Application/Interfaces/`. All controllers use `IDbContextFactory<ApplicationDbContext>` (already standard across the codebase).

| Controller | Endpoints | Service Interface |
|-----------|-----------|-------------------|
| `AuthController` | `POST /api/auth/login`, `/register`, `/refresh`, `/logout`, `GET /api/auth/me` | `UserManager`, `SignInManager` (JWT generation) |
| `ClientsController` | GET, GET by id, POST, PUT, DELETE, GET deleted, POST restore, DELETE permanent | `IClientService` |
| `ProjectsController` | GET list, GET by id, POST, PUT, DELETE, GET deleted, POST restore, DELETE permanent | `IProjectService` |
| `InvoicesController` | GET list, GET by id, POST, PUT, PATCH status, DELETE, GET deleted, POST restore, DELETE permanent | `IInvoiceService` |
| `ReceiptsController` | GET list, GET by id, POST, DELETE, POST restore, DELETE permanent | `IMoneyReceiptService` |
| `DashboardController` | `GET /api/dashboard/stats`, `GET /api/dashboard/recent` | `IDashboardService` |
| `UsersController` | GET list, GET by id, POST, PUT, DELETE, POST roles, GET roles | `IUserService` |
| `ExportController` | `GET /api/export/clients`, `/projects`, `/invoices`, `/reports` | `IExportService` |
| `FiscalYearsController` | GET all, GET current, GET open, POST, PATCH toggle, PATCH set-current | `IFiscalYearService` |
| `AuditLogController` | GET list | `IAuditService` |
| `SettingsController` | `GET /api/settings`, `POST /api/settings` | `IAppSettingService` |
| `BusinessProfileController` | `GET /api/business-profile`, `PUT /api/business-profile` | `IBusinessProfileService` |
| `NotificationsController` | `GET /api/notifications`, `POST /api/notifications/:type/toggle` | `INotificationPreferenceService` |
| `PdfController` | `GET /api/pdf/invoice/:id`, `GET /api/pdf/receipt/:id` | `IInvoicePdfService`, `IReceiptPdfService` |
| `PreferencesController` | `GET /api/preferences/date-display`, `PUT /api/preferences/date-display` | `IUserPreferenceService` |
| `PasskeyController` | `POST /api/passkey/request-options`, `/register`, `/authenticate` | Passkey logic from existing endpoints |
| `FiscalYearController` | GET all, POST, PATCH | `IFiscalYearService` |
| `HealthController` | `GET /api/health` | (simple health check) |

### 1.5 Update `.csproj`

**Remove packages:**
- `MudBlazor` (UI — frontend handles)
- `bunit` (Blazor testing)
- `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore` (optional — only for dev exception pages)

**Add packages:**
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.OpenApi` (or `Swashbuckle.AspNetCore`)

### 1.6 Update `appsettings.json`

Add JWT and CORS configuration:

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
    "Origins": "http://localhost:5173"
  }
}
```

---

## Phase 2: Create React + TypeScript Frontend

### 2.1 Scaffold

```bash
npm create vite@latest frontend -- --template react-ts
cd frontend
npx shadcn@latest init
```

**Tech stack:**
- Vite + React 19 + TypeScript 5
- Tailwind CSS v4 + shadcn/ui (component library)
- React Router v7 (routing)
- Axios (HTTP client)
- Zustand (state management for auth/UI state)
- Lucide React (icons — matches current Material Icons patterns)
- React Query / TanStack Query (server state, caching)
- React Hook Form + Zod (forms + validation)

### 2.2 Directory structure

```
frontend/src/
├── api/                    # Generated API client
│   ├── client.ts           # Axios instance with JWT interceptor
│   ├── endpoints/          # Manual or generated endpoint wrappers
│   └── types/              # TypeScript interfaces matching C# DTOs
├── auth/                   # Authentication
│   ├── AuthContext.tsx      # React context for current user + token
│   ├── ProtectedRoute.tsx  # Route guard component
│   └── TokenManager.ts     # localStorage token read/write/refresh
├── components/ui/          # shadcn/ui generated components
├── components/shared/      # App-specific shared components
│   ├── PageHeader.tsx
│   ├── DataTable.tsx       # Wraps shadcn Table with server-side pagination
│   ├── SearchInput.tsx     # Debounced search
│   ├── ConfirmDialog.tsx
│   ├── StatusBadge.tsx     # Green/Yellow/Red status chip
│   ├── EmptyState.tsx
│   ├── Skeleton.tsx        # Loading skeleton variants
│   ├── FilterPanel.tsx     # Collapsible filter with status + date range
│   ├── ActionButtons.tsx   # View/Edit/Delete group
│   └── FormActions.tsx     # Save/Cancel buttons
├── layouts/
│   ├── AppLayout.tsx       # Sidebar + topbar + main content
│   └── AuthLayout.tsx      # Centered card layout for login/register
├── pages/
│   ├── Dashboard/
│   ├── Clients/            # List, Create, Detail, Edit
│   ├── Projects/           # List, Create, Detail, Edit
│   ├── Invoices/           # List, Create, Detail, Edit
│   ├── Receipts/           # List, Create, Detail
│   ├── Reports/
│   ├── Admin/              # Trash, AuditLog, Users, Settings, FiscalYears
│   ├── Account/            # Manage sub-pages (13)
│   ├── Auth/               # Login, Register, ForgotPassword, etc.
│   └── NotFound.tsx
├── hooks/
│   ├── useAuth.ts
│   ├── useDebounce.ts
│   ├── useLocalStorage.ts
│   └── useWebAuthn.ts      # Passkey browser API wrapper
├── lib/
│   ├── utils.ts            # cn(), formatDate(), formatCurrency()
│   └── constants.ts        # Route paths, role enums
├── styles/
│   └── globals.css         # Tailwind directives + app-specific overrides
├── App.tsx                 # Router setup
└── main.tsx                # Entry point
```

### 2.3 Route-to-page mapping (matching current Blazor routes)

| React Route | Page Component | Auth |
|-------------|---------------|------|
| `/` | `Dashboard` | StaffOrAdmin |
| `/clients` | `ClientsList` | StaffOrAdmin |
| `/clients/new` | `ClientCreate` | StaffOrAdmin |
| `/clients/:id` | `ClientDetail` | StaffOrAdmin |
| `/clients/:id/edit` | `ClientEdit` | StaffOrAdmin |
| `/projects` | `ProjectsList` | StaffOrAdmin |
| `/projects/new` | `ProjectCreate` | StaffOrAdmin |
| `/projects/:id` | `ProjectDetail` | StaffOrAdmin |
| `/projects/:id/edit` | `ProjectEdit` | StaffOrAdmin |
| `/invoices` | `InvoicesList` | StaffOrAdmin |
| `/invoices/new` | `InvoiceCreate` | StaffOrAdmin |
| `/invoices/:id` | `InvoiceDetail` | StaffOrAdmin |
| `/invoices/:id/edit` | `InvoiceEdit` | StaffOrAdmin |
| `/receipts` | `ReceiptsList` | StaffOrAdmin |
| `/receipts/new` | `ReceiptCreate` | StaffOrAdmin |
| `/receipts/:id` | `ReceiptDetail` | StaffOrAdmin |
| `/reports` | `Reports` | AdminOnly |
| `/trash` | `Trash` | AdminOnly |
| `/audit` | `AuditLog` | AdminOnly |
| `/users` | `UsersList` | AdminOnly |
| `/users/new` | `UserCreate` | AdminOnly |
| `/users/:id/edit` | `UserEdit` | AdminOnly |
| `/admin/settings` | `SchedulerSettings` | AdminOnly |
| `/admin/fiscal-years` | `FiscalYears` | AdminOnly |
| `/login` | `Login` | Public |
| `/register` | `Register` | Public |
| `/forgot-password` | `ForgotPassword` | Public |
| `/reset-password` | `ResetPassword` | Public |
| `/account/manage` | `ManageProfile` | Authenticated |
| `/account/manage/email` | `ManageEmail` | Authenticated |
| `/account/manage/password` | `ManagePassword` | Authenticated |
| `/account/manage/2fa` | `Manage2FA` | Authenticated |
| `/account/manage/passkeys` | `ManagePasskeys` | Authenticated |
| `/account/manage/personal-data` | `ManagePersonalData` | Authenticated |
| `/account/notifications` | `Notifications` | Authenticated |
| `/settings/business-profile` | `BusinessProfile` | Authenticated |
| `*` | `NotFound` | Public |

### 2.4 Data fetching pattern

Every API call goes through the generated API client → Axios → JWT interceptor → API controller → service layer.

**Example: Clients list page**

```tsx
// pages/Clients/ClientsList.tsx
import { useQuery } from '@tanstack/react-query'
import { clientApi } from '@/api/endpoints/clients'

export default function ClientsList() {
  const { data, isLoading } = useQuery({
    queryKey: ['clients', { page, search, sort }],
    queryFn: () => clientApi.list({ page, pageSize, search, sort }),
  })

  if (isLoading) return <Skeleton variant="table" />
  return <DataTable columns={columns} data={data.items} pagination={data.pagination} />
}
```

### 2.5 Key forms to implement

| Form | Fields (from C# DTOs) | React Hook Form + Zod |
|------|----------------------|----------------------|
| `ClientCreate` | Name, Email, Company, Phone, Address, PAN | Zod schema matching `CreateClientRequest` |
| `InvoiceCreate` | Client, Project, Item table (dynamic rows), DueDate, Tax, Discount | Complex nested form with dynamic rows |
| `MoneyReceiptCreate` | Client, Invoice selection, Allocated amounts | Multi-step invoice allocation |
| `BusinessProfile` | Company name, address, PAN, phone, logo upload | File upload + text fields |

---

## Phase 3: Authentication Migration

### 3.1 API-side (JWT)

**`AuthController`** key endpoints:

```csharp
[HttpPost("login")]
public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
{
    var user = await _userManager.FindByEmailAsync(request.Email);
    if (user is null) return Unauthorized();

    var valid = await _userManager.CheckPasswordAsync(user, request.Password);
    if (!valid) return Unauthorized();

    var token = GenerateJwt(user);
    var refreshToken = GenerateRefreshToken();

    await _userManager.SetAuthenticationTokenAsync(user, "JWT", "RefreshToken", refreshToken);

    return Ok(new AuthResponse
    {
        Token = token,
        RefreshToken = refreshToken,
        User = MapToUserDto(user)
    });
}
```

**JWT token structure:**
```json
{
  "sub": "user-guid",
  "email": "user@example.com",
  "roles": ["Admin"],
  "exp": 1234567890,
  "iss": "SunyaSuite.Api",
  "aud": "SunyaSuite.Web.Client"
}
```

### 3.2 React-side (Axios interceptor)

```typescript
// api/client.ts
const api = axios.create({ baseURL: '/api' })

api.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  response => response,
  async error => {
    if (error.response?.status === 401) {
      const refreshed = await attemptTokenRefresh()
      if (refreshed) return api.request(error.config)
      // Redirect to login
    }
    return Promise.reject(error)
  }
)
```

### 3.3 Protected routes

```tsx
function ProtectedRoute({ roles }: { roles?: string[] }) {
  const { user, isLoading } = useAuth()

  if (isLoading) return <Skeleton variant="page" />
  if (!user) return <Navigate to="/login" />
  if (roles && !roles.some(r => user.roles.includes(r)))
    return <Navigate to="/" />

  return <Outlet />
}
```

Usage:
```tsx
<Route element={<ProtectedRoute roles={['Admin']} />}>
  <Route path="/users" element={<UsersList />} />
</Route>
```

---

## Phase 4: Design System Port

### 4.1 Tailwind config (from VercelTheme.cs)

```typescript
// tailwind.config.ts
export default {
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#f0fdf4',  // MudBlazor green palette
          500: '#22c55e',
          600: '#16a34a',
        },
        surface: {
          DEFAULT: '#ffffff',
          dark: '#1a1a2e',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'monospace'],
      },
      borderRadius: {
        DEFAULT: '8px',   // matches VercelTheme
      },
      spacing: {
        sidebar: '260px',
        appbar: '56px',
      }
    }
  }
}
```

### 4.2 shadcn/ui theme (matches VercelTheme)

```css
/* styles/globals.css */
@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 0 0% 9%;
    --primary: 142 76% 36%;        /* Green #22c55e */
    --primary-foreground: 0 0% 100%;
    --card: 0 0% 98%;
    --border: 0 0% 90%;
    --radius: 0.5rem;
  }

  .dark {
    --background: 240 10% 12%;     /* Dark mode */
    --foreground: 0 0% 95%;
    --primary: 142 76% 45%;
    --card: 240 10% 16%;
    --border: 240 5% 26%;
  }
}
```

### 4.3 Dark mode

```tsx
// hooks/useDarkMode.ts
export function useDarkMode() {
  const [isDark, setIsDark] = useLocalStorage('sunya-dark-mode', false)

  useEffect(() => {
    document.documentElement.classList.toggle('dark', isDark)
  }, [isDark])

  return { isDark, toggle: () => setIsDark(!isDark) }
}
```

### 4.4 Key shared components

| Blazor Component | React Equivalent |
|-----------------|------------------|
| `<PageHeader Title="..." />` | `<PageHeader title="..." icon={...} subtitle="..." />` |
| `<StatusChip Status="@status" />` | `<StatusBadge status={status} />` |
| `<LoadingSkeleton Variant="..." />` | `<Skeleton variant="table" />` |
| `<ConfirmDeleteDialog>` | `<ConfirmDialog title="Delete" onConfirm={...} />` |
| `<DetailInfoCard Fields="@fields" />` | `<InfoCard fields={fields} />` |
| `<DataTable>` with server-side sort/search/pagination | `<DataTable columns={...} data={...} serverSide={...} />` |
| `<FilterPanel>` with status + date filters | `<FilterPanel>` with same children pattern |
| `<ActionButtons ViewHref EditHref OnDelete>` | `<ActionButtons onView onEdit onDelete />` |

---

## Phase 5: Deployment & CI/CD

### 5.1 Development workflow

```bash
# Terminal 1: .NET API
cd src/SunyaSuite.Web
dotnet watch run

# Terminal 2: React frontend
cd frontend
npm run dev

# API at https://localhost:5001, Frontend at http://localhost:5173
# Vite proxy forwards /api/* to the .NET backend
```

### 5.2 Vite proxy config

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': 'https://localhost:5001',
      '/health': 'https://localhost:5001',
    }
  }
})
```

### 5.3 Production build

```bash
# Build React
cd frontend && npm run build    # outputs to frontend/dist/

# Option A: Serve from .NET (copy dist/ to wwwroot/)
dotnet publish src/SunyaSuite.Web
# Or:

# Option B: Serve via nginx/caddy (separate containers)
# docker-compose with api + frontend services
```

---

## Migration Order (Execution Sequence)

| Step | Task | Files affected | Dependencies |
|------|------|---------------|--------------|
| 1 | Add JWT + Swagger + CORS packages to `.csproj` + `Directory.Packages.props` | 2 | None |
| 2 | Create `AuthController` with JWT login/register/refresh | 1 | Step 1 |
| 3 | Create API controllers (Clients, Projects, Invoices, Receipts, Users) | 5 | Step 1 |
| 4 | Create remaining API controllers (Dashboard, Export, Audit, Settings, etc.) | 10 | Step 1 |
| 5 | Rewrite `Program.cs` — remove Blazor, add API pipeline | 1 | Steps 1-4 |
| 6 | Remove all Blazor files (`.razor`, `.razor.js`, Themes, wwwroot CSS/JS) | ~95 | Step 5 (code won't compile until Program.cs is updated) |
| 7 | Scaffold React project + install dependencies | package.json | None (parallel to Steps 1-6) |
| 8 | Build React auth infrastructure (AuthContext, TokenManager, ProtectedRoute) | ~5 | Step 2 (API login must exist for testing) |
| 9 | Build shared components (DataTable, PageHeader, StatusBadge, etc.) | ~10 | Step 8 |
| 10 | Build layouts (AppLayout with sidebar, AuthLayout) | 2 | Steps 8-9 |
| 11 | Build Clients pages (List, Create, Detail, Edit) | 4 | Steps 8-10, Step 3 (API endpoints) |
| 12 | Build Projects pages | 4 | Steps 8-10, Step 3 |
| 13 | Build Invoices pages (List, Create, Detail, Edit — most complex) | 4 | Steps 8-10, Step 3 |
| 14 | Build Receipts pages | 3 | Steps 8-10, Step 3 |
| 15 | Build Admin pages (Users, Trash, Audit, Settings, FiscalYears) | 6 | Steps 8-10, Steps 3-4 |
| 16 | Build Account/Manage pages (13 sub-pages) | 13 | Steps 8-10, Steps 2 |
| 17 | Build Auth pages (Login, Register, ForgotPassword, ResetPassword) | 4 | Steps 8-10, Step 2 |
| 18 | Build Dashboard, Reports, Notifications, BusinessProfile | 4 | Steps 8-10, Step 4 |
| 19 | Passkey/WebAuthn browser integration (port `PasskeySubmit.razor.js`) | 1 | Steps 8-10, Step 4 |
| 20 | Build + test end-to-end | — | All steps |

---

## File Count Summary

| Category | Added | Removed | Modified |
|----------|-------|---------|----------|
| .NET API controllers | ~18 | 0 | 0 |
| `Program.cs` changes | 0 | 0 | 1 |
| `.csproj` changes | 0 | 0 | 1 |
| `appsettings.json` | 0 | 0 | 1 |
| Blazor Razor files | 0 | ~89 | 0 |
| Blazor C# UI infra | 0 | ~6 | 0 |
| MudBlazor/Vercel | 0 | ~1 | 0 |
| `wwwroot` files | 0 | ~3 | 0 |
| React pages | ~45 | 0 | 0 |
| React shared components | ~20 | 0 | 0 |
| React auth + infra | ~10 | 0 | 0 |
| React config + styles | ~10 | 0 | 0 |
| **Total** | **~103** | **~99** | **3** |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Auth migration (cookie → JWT) breaks existing sessions | High | Run both auth schemes in parallel during migration. Or plan a forced re-login for all users on cutover day |
| Invoice creation form is complex (dynamic line items, tax calc, client invoicing) | Medium | Build as a dedicated multi-step form component. Port `Invoice.Recalculate()` logic to frontend for real-time totals |
| Passkey/WebAuthn browser API is tightly coupled to Blazor's custom element | Medium | Wrap in a `useWebAuthn()` hook. The underlying WebAuthn API calls (`navigator.credentials.create/get`) work identically in any browser JS |
| PDF/Excel generation stays server-side — download flow changes | Low | API returns `FileStreamResult`. Frontend uses `response.blob()` + `URL.createObjectURL()` — standard pattern |
| ~100 new files is a large codebase to bootstrap | Medium | Use OpenAPI codegen + shadcn/ui CLI to accelerate scaffolding. Prioritize by functional area (start with Clients → Invoices → Auth) |
