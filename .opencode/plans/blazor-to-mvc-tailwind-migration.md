# Migration Plan: Blazor Server → ASP.NET Core MVC + Tailwind CSS

## Overview

**Goal:** Remove all Blazor UI (89 `.razor` files, MudBlazor, VercelTheme), replace with ASP.NET Core MVC Razor views + Tailwind CSS. All UI is custom (no MudBlazor). Interactivity via HTMX + Alpine.js.

**Keep unchanged:** `SunyaSuite.Domain`, `SunyaSuite.Application`, `SunyaSuite.Infrastructure` (same services, DbContext, migrations — zero changes)

**Web project transformation:**

| Current | Target |
|---------|--------|
| `builder.Services.AddRazorComponents().AddInteractiveServerComponents()` | `builder.Services.AddControllersWithViews()` |
| `Components/` (89 `.razor` files) | `Views/` (~60 `.cshtml` files) |
| `Components/Layout/MainLayout.razor` | `Views/Shared/_Layout.cshtml` |
| MudBlazor components | Custom Tailwind HTML + partials |
| MudBlazor form validation | `TagHelpers` + ModelState + HTMX |
| MudBlazor MudTable | Tailwind table + HTMX partial swap |
| MudBlazor MudChart (Dashboard) | Chart.js |
| MudBlazor MudDatePicker | Flatpickr or native `<input type="date">` |
| Blazor SignalR circuit | Standard HTTP request-response |
| `@rendermode InteractiveServer` | N/A (server-rendered HTML) |
| Cookie auth (Identity) | Cookie auth (Identity) — **unchanged** |

---

## Phase 0: Project Setup & Config

### 0.1 Update `.csproj`

**Remove packages:**
- `MudBlazor`

**Keep packages (already in project):**
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.Extensions.Identity.Stores`
- `Serilog.AspNetCore`
- All EF Core + Npgsql packages
- QuestPDF, ClosedXML, QRCoder, MailKit

**No new packages needed** — MVC ships with ASP.NET Core. HTMX and Alpine.js are loaded via CDN (no npm needed).

### 0.2 Install Tailwind CSS

**Option A: CDN (fastest setup)**
```html
<script src="https://cdn.tailwindcss.com"></script>
```
No build step. Works for ~60 views. Good for an internal app.

**Option B: npm + CLI build (for production optimization)**
```bash
npm init -y
npm install -D tailwindcss @tailwindcss/cli
npx tailwindcss init
```
- Configure `tailwind.config.js` with view paths
- Use `npx tailwindcss -i wwwroot/css/app.css -o wwwroot/css/app.min.css` as build step
- Integrate into `.csproj` with a pre-build target

**Recommendation:** Start with Option A (CDN) for migration speed. Switch to Option B before production deployment.

### 0.3 Configure HTMX + Alpine.js

```html
<!-- _Layout.cshtml <head> -->
<script src="https://unpkg.com/htmx.org@2.0.4"></script>
<script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.14.8/dist/cdn.min.js"></script>
```

HTMX handles all AJAX interactions (table pagination, filter panels, search, inline actions).  
Alpine.js handles client-only state (dark mode toggle, responsive sidebar, confirm dialogs, client-side form state).

---

## Phase 1: Convert Program.cs — Blazor → MVC

### Remove (exact lines from current Program.cs)

```csharp
// Lines 2-3: Remove Blazor-specific usings
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

// Line 5: Remove MudBlazor
using MudBlazor.Services;

// Lines 14-15: Remove component roots
using SunyaSuite.Web.Components;
using SunyaSuite.Web.Components.Account;

// Line 28-29: Replace
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
// → builder.Services.AddControllersWithViews();

// Line 31: Remove
builder.Services.AddMudServices();

// Lines 33-35: Remove (Blazor-specific auth plumbing)
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// Lines 53-59: Remove (Blazor circuit config)
builder.Services.Configure<CircuitOptions>(...)

// Lines 99-105: Replace
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
// → app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

// Line 107: Remove
app.MapAdditionalIdentityEndpoints();
```

### Keep unchanged

```csharp
// QuestPDF license, Serilog, Identity core, policies, AddInfrastructure,
// AddHostedService<OverdueBackgroundService>(), Exception handling,
// Health checks, EF migration + seed — all stay
```

---

## Phase 2: Layout — Site-wide Structure

### 2.1 `Views/Shared/_Layout.cshtml`

Replaces `Components/App.razor` + `Components/Layout/MainLayout.razor`.

```html
<!DOCTYPE html>
<html lang="en" x-data="{ dark: localStorage.getItem('sunya-dark-mode') === 'true' }" :class="{ 'dark': dark }">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] — SunyaSuite</title>
    <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap" rel="stylesheet">
    <script src="https://unpkg.com/htmx.org@2.0.4"></script>
    <script defer src="https://cdn.jsdelivr.net/npm/alpinejs@3.14.8/dist/cdn.min.js"></script>
    <script src="https://cdn.tailwindcss.com"></script>
    <script>
        tailwind.config = {
            darkMode: 'class',
            theme: {
                extend: {
                    fontFamily: { sans: ['Inter', 'system-ui', 'sans-serif'] },
                    colors: {
                        primary: { 50: '#f0fdf4', 500: '#22c55e', 600: '#16a34a' },
                        surface: { DEFAULT: '#ffffff', dark: '#1a1a2e' }
                    }
                }
            }
        }
    </script>
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body class="bg-gray-50 dark:bg-[#1a1a2e] text-gray-900 dark:text-gray-100 font-sans">
    <!-- Responsive sidebar + topbar (Alpine.js toggled) -->
    <div x-data="{ sidebarOpen: window.innerWidth >= 1280 }">
        <!-- Mobile overlay -->
        <div x-show="sidebarOpen" @@click="sidebarOpen = false" class="fixed inset-0 bg-black/50 z-40 xl:hidden"></div>

        <!-- Sidebar -->
        <aside class="fixed top-0 left-0 z-50 h-full w-64 bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-700 transform transition-transform"
               :class="sidebarOpen ? 'translate-x-0' : '-translate-x-full'"
               class="xl:translate-x-0">
            <!-- Brand -->
            <div class="h-14 flex items-center px-6 border-b border-gray-200 dark:border-gray-700">
                <a href="/" class="text-lg font-bold text-primary-600 dark:text-primary-500">SunyaSuite</a>
            </div>
            <!-- Navigation -->
            <nav class="p-4 space-y-1">
                @await Html.PartialAsync("_NavMenu")
            </nav>
        </aside>

        <!-- Main content area -->
        <div class="xl:ml-64">
            <!-- Top bar -->
            <header class="h-14 flex items-center justify-between px-4 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700">
                <button @@click="sidebarOpen = !sidebarOpen" class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800">
                    <!-- hamburger icon -->
                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/></svg>
                </button>
                <div class="flex items-center gap-3">
                    <!-- Dark mode toggle -->
                    <button @@click="dark = !dark; localStorage.setItem('sunya-dark-mode', dark)"
                            class="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800">
                        <template x-if="!dark">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">...</svg>
                        </template>
                        <template x-if="dark">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">...</svg>
                        </template>
                    </button>
                    <!-- User menu -->
                    <partial name="_UserMenu" />
                </div>
            </header>

            <!-- Page content -->
            <main class="p-6">
                @RenderBody()
            </main>
        </div>
    </div>

    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

### 2.2 `Views/Shared/_NavMenu.cshtml`

```html
<!-- Static nav menu with role-based sections -->
@if (User.Identity?.IsAuthenticated == true)
{
    <!-- Main section -->
    <a href="/" class="flex items-center gap-3 px-3 py-2 rounded-lg ...">
        <svg class="w-5 h-5">...</svg> Dashboard
    </a>
    <a href="/Clients" class="flex items-center gap-3 px-3 py-2 rounded-lg ...">
        <svg class="w-5 h-5">...</svg> Clients
    </a>
    <!-- ... -->

    @if (User.IsInRole("Admin"))
    {
        <div class="my-2 border-t border-gray-200 dark:border-gray-700"></div>
        <span class="px-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Admin</span>
        <a href="/Reports">Reports</a>
        <a href="/Users">Users</a>
        <!-- ... -->
    }
}
```

### 2.3 `Views/Shared/_AuthLayout.cshtml`

Minimal layout for login/register pages (no sidebar).

```html
<!DOCTYPE html>
<html>
<head>...</head>
<body class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-[#1a1a2e]">
    <div class="w-full max-w-md px-6">
        <div class="text-center mb-8">
            <h1 class="text-2xl font-bold text-primary-600">SunyaSuite</h1>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-8">
            @RenderBody()
        </div>
    </div>
</body>
</html>
```

---

## Phase 3: Route Structure & Controllers

### 3.1 Controllers (1 per functional area)

| Controller | Actions | Replaces Blazor Page(s) |
|-----------|---------|------------------------|
| `HomeController` | `Index` (dashboard) | `Pages/Home.razor` |
| `ClientsController` | `Index`, `Create`, `Detail`, `Edit`, `Delete`, `Restore`, `PermanentDelete` | `Pages/Clients/*` |
| `ProjectsController` | `Index`, `Create`, `Detail`, `Edit`, `Delete`, `Restore`, `PermanentDelete` | `Pages/Projects/*` |
| `InvoicesController` | `Index`, `Create`, `Detail`, `Edit`, `UpdateStatus`, `Delete`, `Restore`, `PermanentDelete` | `Pages/Invoices/*` |
| `ReceiptsController` | `Index`, `Create`, `Detail`, `Delete`, `Restore`, `PermanentDelete` | `Pages/Receipts/*` |
| `UsersController` | `Index`, `Create`, `Edit`, `Delete`, `AssignRoles` | `Pages/Users/*` |
| `DashboardController` | `Stats`, `Recent` (API partials for HTMX) | Dashboard widgets |
| `ExportController` | `Clients`, `Projects`, `Invoices`, `Reports` | Export functionality |
| `AuditLogController` | `Index` | `Pages/AuditLog/Index.razor` |
| `TrashController` | `Index`, `Restore`, `PermanentDelete` | `Pages/Trash.razor` |
| `SettingsController` | `Index`, `Update` (scheduler settings) | `Pages/Admin/Settings.razor` |
| `FiscalYearController` | `Index`, `Create`, `ToggleOpen`, `SetCurrent` | `Pages/Admin/FiscalYears.razor` |
| `BusinessProfileController` | `Index`, `Save`, `UploadLogo` | `Pages/Settings/BusinessProfile.razor` |
| `NotificationsController` | `Index`, `Toggle` | `Pages/Account/Notifications.razor` |
| `AccountController` | `Index`, `Email`, `Password`, `TwoFactor`, `Passkeys`, `PersonalData` (manage pages) | `Pages/Account/Manage/*` |
| `AuthController` | `Login`, `Register`, `ForgotPassword`, `ResetPassword`, `ConfirmEmail`, `Logout` | `Pages/Account/Pages/*` (auth pages) |

### 3.2 Controller Pattern

```csharp
public class ClientsController : Controller
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int page = 1, int pageSize = 10,
        string? sortLabel, string? sortDirection, string? search,
        List<ClientStatus>? statuses,
        DateTime? registeredOnFrom, DateTime? registeredOnTo,
        CancellationToken ct)
    {
        var filter = new ClientFilterDto
        {
            Statuses = statuses,
            RegisteredOnFrom = registeredOnFrom,
            RegisteredOnTo = registeredOnTo,
        };

        var result = await _clientService.GetPagedAsync(
            page, pageSize, sortLabel, sortDirection, search, filter, ct);

        // For HTMX: if request is partial, return only the table partial
        if (Request.IsHtmx())
            return PartialView("_ClientsTable", result);

        return View(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var client = await _clientService.GetByIdAsync(id, ct);
        if (client is null) return NotFound();
        return View(client);
    }

    [HttpGet("create")]
    public IActionResult Create() => View();

    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateClientRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(request);
        await _clientService.CreateAsync(request, ct);
        TempData["Success"] = "Client created successfully.";
        return RedirectToAction(nameof(Index));
    }

    // HTMX endpoint for inline delete confirmation + action
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _clientService.DeleteAsync(id, ct);
        return Ok();
    }
}
```

---

## Phase 4: Views — Mapping Razor Components to CSHTML

### 4.1 Clients Index (replaces `Pages/Clients/Index.razor`)

```html
@model PagedResult<ClientListItemDto>
@{
    ViewData["Title"] = "Clients";
}

<!-- PageHeader equivalent -->
<div class="flex items-center justify-between mb-6">
    <div>
        <h1 class="text-2xl font-semibold text-gray-900 dark:text-gray-100">Clients</h1>
    </div>
    <a href="@Url.Action("Create")" class="inline-flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
        Add Client
    </a>
</div>

<!-- IndexToolbar equivalent -->
<div class="flex items-center gap-3 mb-4">
    <!-- Search: HTMX triggers table re-render on input -->
    <input type="text" name="search" placeholder="Search by name, email, or company..."
           class="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800"
           hx-get="@Url.Action("Index")" hx-trigger="keyup changed delay:300ms" hx-target="#clients-table" hx-include="[name='page'],[name='sortLabel'],[name='sortDirection']" />

    <!-- Filter panel toggle -->
    <button hx-get="@Url.Action("FilterPanel")" hx-target="#filter-panel" hx-swap="innerHTML"
            class="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">
        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z"/></svg>
        Filters
    </button>

    <!-- Export -->
    <button hx-get="@Url.Action("ExportClients")" hx-trigger="click"
            class="px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">
        Export
    </button>
</div>

<!-- Filter panel (collapsible, loaded via HTMX) -->
<div id="filter-panel"></div>

<!-- Table (server-rendered, HTMX target) -->
<div id="clients-table">
    @await Html.PartialAsync("_ClientsTable", Model)
</div>
```

### 4.2 `_ClientsTable.cshtml` (partial — HTMX swap target)

```html
@model PagedResult<ClientListItemDto>

<div class="overflow-x-auto bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700">
    <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
                <th><a href="#" hx-get="@Url.Action("Index", new { sortLabel = "name", sortDirection = ViewBag.SortDir == "asc" ? "desc" : "asc" })" hx-target="#clients-table">Name</a></th>
                <th>Email</th>
                <th>Company</th>
                <th>
                    <a href="#" hx-get="@Url.Action("Index", new { sortLabel = "status", sortDirection = ViewBag.SortDir == "asc" ? "desc" : "asc" })" hx-target="#clients-table">Status</a>
                </th>
                <th>Actions</th>
            </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            @foreach (var client in Model.Items)
            {
                <tr class="hover:bg-gray-50 dark:hover:bg-gray-800">
                    <td class="px-4 py-3"><a href="@Url.Action("Detail", new { id = client.Id })" class="text-primary-600 hover:underline">@client.Name</a></td>
                    <td class="px-4 py-3">@client.Email</td>
                    <td class="px-4 py-3">@client.Company</td>
                    <td class="px-4 py-3">
                        <span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium
                            @(client.Status == "Green" ? "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200" :
                              client.Status == "Yellow" ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200" :
                              "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200")">
                            @client.Status
                        </span>
                    </td>
                    <td class="px-4 py-3">
                        <div class="flex items-center gap-2">
                            <a href="@Url.Action("Detail", new { id = client.Id })" class="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">View</a>
                            <a href="@Url.Action("Edit", new { id = client.Id })" class="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">Edit</a>
                            <button hx-delete="@Url.Action("Delete", new { id = client.Id })"
                                    hx-confirm="Delete this client?"
                                    hx-target="closest tr" hx-swap="outerHTML"
                                    class="p-1 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded">
                                Delete
                            </button>
                        </div>
                    </td>
                </tr>
            }
        </tbody>
    </table>
</div>

<!-- Pagination -->
@if (Model.Total > 0)
{
    <div class="flex items-center justify-between mt-4">
        <span class="text-sm text-gray-600 dark:text-gray-400">@Model.Total total</span>
        <div class="flex gap-1">
            @for (int i = 1; i <= Math.Ceiling((double)Model.Total / ViewBag.PageSize); i++)
            {
                <a href="#" hx-get="@Url.Action("Index", new { page = i })" hx-target="#clients-table"
                   class="px-3 py-1 rounded @(i == ViewBag.Page ? "bg-primary-600 text-white" : "hover:bg-gray-100 dark:hover:bg-gray-800")">
                    @i
                </a>
            }
        </div>
    </div>
}
```

### 4.3 Invoice Create (the most complex page — MudBlazor → Tailwind + Alpine.js)

```html
@model CreateInvoiceRequest
@{
    ViewData["Title"] = "Create Invoice";
}

<div class="max-w-4xl mx-auto" x-data="invoiceForm()">
    <h1 class="text-2xl font-semibold mb-6">Create Invoice</h1>

    <form method="post" @@submit.prevent="submitForm">
        @Html.AntiForgeryToken()

        <!-- Client + Bill Type row -->
        <div class="grid grid-cols-2 gap-4 mb-4">
            <div>
                <label class="block text-sm font-medium mb-1">Client</label>
                <select asp-for="ClientId" asp-items="ViewBag.Clients"
                        class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800">
                </select>
            </div>
            <div>
                <label class="block text-sm font-medium mb-1">Due Date</label>
                <input asp-for="DueDate" type="date"
                       class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800" />
            </div>
        </div>

        <!-- Bill Type toggle -->
        <div class="flex gap-2 mb-4">
            <button type="button" @@click="billType = 'VatBill'"
                    :class="billType === 'VatBill' ? 'bg-primary-600 text-white' : 'bg-gray-100 dark:bg-gray-800'"
                    class="px-4 py-2 rounded-lg font-medium">
                VAT Bill
            </button>
            <button type="button" @@click="billType = 'PanBill'"
                    :class="billType === 'PanBill' ? 'bg-primary-600 text-white' : 'bg-gray-100 dark:bg-gray-800'"
                    class="px-4 py-2 rounded-lg font-medium">
                PAN Bill
            </button>
        </div>

        <!-- Items table (dynamic rows via Alpine.js) -->
        <div class="mb-4">
            <label class="block text-sm font-medium mb-2">Items</label>
            <table class="w-full border border-gray-200 dark:border-gray-700 rounded-lg">
                <thead>
                    <tr class="bg-gray-50 dark:bg-gray-800 text-sm">
                        <th class="px-3 py-2 text-left">Description</th>
                        <th class="px-3 py-2 text-left w-24">Qty</th>
                        <th class="px-3 py-2 text-left w-24">Unit</th>
                        <th class="px-3 py-2 text-right w-32">Unit Price</th>
                        <th class="px-3 py-2 text-right w-32">Total</th>
                        <th class="px-3 py-2 w-12"></th>
                    </tr>
                </thead>
                <tbody>
                    <template x-for="(item, idx) in items" :key="idx">
                        <tr>
                            <td class="px-3 py-2">
                                <input type="text" x-model="item.description" :name="`Items[${idx}].Description`" required
                                       class="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded" />
                            </td>
                            <td class="px-3 py-2">
                                <input type="number" x-model="item.quantity" :name="`Items[${idx}].Quantity`" min="0.01" step="0.01"
                                       @@input="recalc()"
                                       class="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded" />
                            </td>
                            <td class="px-3 py-2">
                                <input type="text" x-model="item.unit" :name="`Items[${idx}].Unit`"
                                       class="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded" />
                            </td>
                            <td class="px-3 py-2">
                                <input type="number" x-model="item.unitPrice" :name="`Items[${idx}].UnitPrice`" min="0" step="0.01"
                                       @@input="recalc()"
                                       class="w-full px-2 py-1 border border-gray-300 dark:border-gray-600 rounded text-right" />
                            </td>
                            <td class="px-3 py-2 text-right font-medium" x-text="lineTotal(idx).toFixed(2)"></td>
                            <td class="px-3 py-2">
                                <button type="button" @@click="removeItem(idx)" x-show="items.length > 1"
                                        class="text-red-500 hover:text-red-700">✕</button>
                            </td>
                        </tr>
                    </template>
                </tbody>
            </table>
            <button type="button" @@click="addItem()"
                    class="mt-2 px-3 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">
                + Add Item
            </button>
        </div>

        <!-- Tax & Discount -->
        <div class="grid grid-cols-2 gap-4 mb-4">
            <div>
                <label class="block text-sm font-medium mb-1">Tax Rate (%)</label>
                <input type="number" x-model="taxRate" :disabled="billType === 'PanBill'"
                       class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800" />
            </div>
            <div>
                <label class="block text-sm font-medium mb-1">Discount</label>
                <input type="number" x-model="discount"
                       class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800" />
            </div>
        </div>

        <!-- Summary -->
        <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-4 mb-6 space-y-1 text-sm">
            <div class="flex justify-between"><span>Subtotal</span><span x-text="subtotal().toFixed(2)"></span></div>
            <template x-if="billType === 'VatBill' && !isAbbreviated">
                <div class="flex justify-between"><span>VAT (13%)</span><span x-text="vat().toFixed(2)"></span></div>
            </template>
            <template x-if="discount > 0">
                <div class="flex justify-between"><span>Discount</span><span x-text="`-${discount}`"></span></div>
            </template>
            <div class="flex justify-between font-semibold text-base pt-2 border-t border-gray-200 dark:border-gray-700">
                <span>Total</span><span x-text="total().toFixed(2)"></span>
            </div>
        </div>

        <div class="flex gap-3">
            <button type="submit" class="px-6 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 font-medium">Create Invoice</button>
            <a href="@Url.Action("Index")" class="px-6 py-2 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800">Cancel</a>
        </div>
    </form>
</div>

<!-- Alpine.js component for invoice form -->
<script>
    function invoiceForm() {
        return {
            billType: 'VatBill',
            isAbbreviated: false,
            taxRate: 13,
            discount: 0,
            items: [{ description: '', quantity: 1, unit: 'pcs', unitPrice: 0 }],
            addItem() { this.items.push({ description: '', quantity: 1, unit: 'pcs', unitPrice: 0 }) },
            removeItem(idx) { if (this.items.length > 1) this.items.splice(idx, 1) },
            lineTotal(idx) {
                const item = this.items[idx];
                return (parseFloat(item.quantity) || 0) * (parseFloat(item.unitPrice) || 0);
            },
            subtotal() { return this.items.reduce((sum, _, idx) => sum + this.lineTotal(idx), 0) },
            vat() { return this.billType === 'VatBill' && !this.isAbbreviated ? this.subtotal() * 0.13 : 0 },
            total() { return this.subtotal() - this.discount + this.vat() },
            submitForm() {
                // HTMX override: submit as form body
                const form = event.target;
                htmx.ajax('POST', form.action, { source: form, target: '#main-content', swap: 'innerHTML' });
            }
        }
    }
</script>
```

### 4.4 HTMX Extension: Detect HTMX requests

```csharp
// Helpers/HtmxExtensions.cs
public static class HtmxExtensions
{
    public static bool IsHtmx(this HttpRequest request)
        => request.Headers["HX-Request"] == "true";
}
```

---

## Phase 5: Auth Pages — Identity (Unchanged flow, new views)

Identity is already configured in Program.cs with cookie auth — this stays. The only change is replacing Blazor auth pages with MVC views.

| Blazor Page | MVC Action + View |
|------------|-------------------|
| `Login.razor` | `AuthController.Login()` → `Views/Auth/Login.cshtml` |
| `Register.razor` | `AuthController.Register()` → `Views/Auth/Register.cshtml` |
| `ForgotPassword.razor` | `AuthController.ForgotPassword()` → `Views/Auth/ForgotPassword.cshtml` |
| `ResetPassword.razor` | `AuthController.ResetPassword()` → `Views/Auth/ResetPassword.cshtml` |
| `Manage/*` (13 pages) | `AccountController.*()` → `Views/Account/Manage/*.cshtml` |

**Login view example:**

```html
@model LoginViewModel
@{
    Layout = "_AuthLayout";
    ViewData["Title"] = "Log in";
}

<h2 class="text-xl font-semibold mb-1">Log in</h2>
<p class="text-sm text-gray-500 dark:text-gray-400 mb-6">Use your email and password to sign in.</p>

@if (!string.IsNullOrEmpty(Model?.ErrorMessage))
{
    <div class="mb-4 p-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-red-700 dark:text-red-300 text-sm">
        @Model.ErrorMessage
    </div>
}

<form method="post" asp-action="Login">
    @Html.AntiForgeryToken()
    <input type="hidden" name="ReturnUrl" value="@Context.Request.Query["ReturnUrl"]" />

    <div class="space-y-4">
        <div>
            <label asp-for="Email" class="block text-sm font-medium mb-1"></label>
            <input asp-for="Email" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800"
                   autocomplete="username" placeholder="name@example.com" />
            <span asp-validation-for="Email" class="text-red-500 text-xs"></span>
        </div>
        <div>
            <label asp-for="Password" class="block text-sm font-medium mb-1"></label>
            <input asp-for="Password" type="password" class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800"
                   autocomplete="current-password" placeholder="Enter your password" />
            <span asp-validation-for="Password" class="text-red-500 text-xs"></span>
        </div>
        <div class="flex items-center gap-2">
            <input asp-for="RememberMe" type="checkbox" class="rounded border-gray-300" />
            <label asp-for="RememberMe" class="text-sm">Remember me</label>
        </div>
    </div>

    <button type="submit" class="w-full mt-6 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 font-medium">Log in</button>

    <div class="mt-4 text-sm space-y-2">
        <a asp-action="ForgotPassword" class="text-primary-600 hover:underline">Forgot your password?</a>
        <br/>
        <a asp-action="Register" class="text-primary-600 hover:underline">Register as a new user</a>
    </div>
</form>
```

---

## Phase 6: Shared Components — MudBlazor → Tailwind Mapping

| MudBlazor Component | Tailwind Equivalent |
|--------------------|--------------------|
| `<MudContainer MaxWidth="MaxWidth.Small">` | `<div class="max-w-md mx-auto px-4">` |
| `<MudPaper Class="pa-8" Elevation="0">` | `<div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-8">` |
| `<MudButton Variant="Filled" Color="Primary">` | `<button class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 font-medium">` |
| `<MudTextField Label="..." />` | `<div><label class="block text-sm font-medium mb-1">...</label><input class="w-full px-3 py-2 border rounded-lg ..."></div>` |
| `<MudSelect Label="...">` | `<select class="w-full px-3 py-2 border rounded-lg bg-white dark:bg-gray-800">` |
| `<MudDatePicker>` | `<input type="date" class="w-full px-3 py-2 border rounded-lg">` |
| `<MudSwitch>` | `<label class="relative inline-flex items-center cursor-pointer"><input type="checkbox" class="sr-only peer"><div class="w-11 h-6 bg-gray-200 rounded-full peer peer-checked:bg-primary-600"></div></label>` |
| `<MudTable>` | Tailwind table (lines above in section 4.2) |
| `<MudChip>` / `StatusChip` | `<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-{green/yellow/red}-100 text-{green/yellow/red}-800">` |
| `<MudProgressLinear>` | `<div class="w-full bg-gray-200 rounded-full h-2"><div class="bg-primary-600 h-2 rounded-full" style="width: {value}%"></div></div>` |
| `<MudAlert Severity="Error">` | `<div class="p-3 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">` |
| `<MudLink>` | `<a class="text-primary-600 hover:underline">` |
| `<MudIconButton>` | `<button class="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">` |
| `<MudGrid>` / `<MudItem>` | `<div class="grid grid-cols-1 md:grid-cols-2 gap-4">` |
| `<MudTablePager>` | Custom pagination (section 4.2) |
| `<MudDialog>` | Alpine.js `x-dialog` or `<dialog>` element + HTMX |

---

## Phase 7: JS Dependencies — Replace MudBlazor JS

### 7.1 What replaces MudBlazor JS

| MudBlazor Feature | Replacement |
|------------------|-------------|
| Component rendering | Server-rendered HTML |
| Form validation | `jquery.validate` + `jquery.validate.unobtrusive` (standard ASP.NET Core MVC) |
| Date picker | Native `<input type="date">` or Flatpickr |
| Snackbar/Toast | Alpine.js toast component |
| Dialog/Modal | `<dialog>` HTML element + Alpine.js |
| Charts (Dashboard) | Chart.js |
| Icons | Lucide (inline SVG) or Heroicons (inline SVG) |

### 7.2 `site.js` — Custom scripts

```javascript
// Dark mode initialization (runs before Alpine.js)
(function() {
    const dark = localStorage.getItem('sunya-dark-mode') === 'true';
    if (dark) document.documentElement.classList.add('dark');
})();

// File download helper (replaces scripts.js downloadFile)
window.downloadFile = function(fileName, base64, mimeType) {
    const byteCharacters = atob(base64);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++)
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
};
```

---

## Phase 8: Remaining Pages — Complete List

### Pages to build (~60 total CSHTML files):

| Functional Area | Views |
|----------------|-------|
| **Layouts** | `_Layout.cshtml`, `_AuthLayout.cshtml`, `_NavMenu.cshtml`, `_UserMenu.cshtml` |
| **Auth** | `Login`, `Register`, `ForgotPassword`, `ResetPassword`, `ConfirmEmail`, `ResendConfirmation`, `Lockout`, `AccessDenied`, `ExternalLogin`, `LoginWith2fa`, `LoginWithRecoveryCode`, `RegisterConfirmation` |
| **Account/Manage** | `Index`, `Email`, `ChangePassword`, `ExternalLogins`, `TwoFactorAuthentication`, `EnableAuthenticator`, `Disable2fa`, `ResetAuthenticator`, `GenerateRecoveryCodes`, `Passkeys`, `RenamePasskey`, `PersonalData`, `DeletePersonalData`, `SetPassword` |
| **Clients** | `Index`, `Create`, `Detail`, `Edit`, `_ClientsTable` (partial), `_FilterPanel` (partial) |
| **Projects** | `Index`, `Create`, `Detail`, `Edit`, `_ProjectsTable` (partial), `_FilterPanel` (partial) |
| **Invoices** | `Index`, `Create`, `Detail`, `Edit`, `_InvoicesTable` (partial), `_FilterPanel` (partial) |
| **Receipts** | `Index`, `Create`, `Detail`, `_ReceiptsTable` (partial) |
| **Admin** | `Trash`, `AuditLog`, `Users/Index`, `Users/Create`, `Users/Edit`, `Settings/Index`, `FiscalYears/Index`, `_FiscalYearDialog` (partial) |
| **Dashboard** | `Index`, `_StatsCards` (partial), `_Charts` (partial), `_RecentInvoices` (partial) |
| **Other** | `NotFound`, `Error`, `Home/Index`, `Notifications/Index`, `BusinessProfile/Index` |

**Total: ~60 views** (compared to 89 Razor components — many Blazor components are small shared partials that collapse into fewer Tailwind partials)

---

## Phase 9: Migration Order (20 Steps)

| Step | Task | Depends on |
|------|------|-----------|
| 1 | Update `.csproj` — remove MudBlazor | None |
| 2 | Update `Program.cs` — replace Blazor with MVC | None |
| 3 | Create `_Layout.cshtml` + `_AuthLayout.cshtml` (Tailwind) | Step 2 |
| 4 | Create `_NavMenu.cshtml` | Step 3 |
| 5 | Create `Helpers/HtmxExtensions.cs` + TagHelpers | None |
| 6 | Build Controllers (Clients, Projects, Invoices, Receipts, Users) | Step 2 |
| 7 | Build Controllers (Auth, Account, Dashboard, remaining) | Step 2 |
| 8 | Build `site.css` + `site.js` | None |
| 9 | Build Auth views (Login, Register, ForgotPassword, etc.) | Steps 3-4, 7 |
| 10 | Build Account/Manage views (13 sub-pages) | Steps 3-4, 7 |
| 11 | Build Clients views (Index + partials, Create, Detail, Edit) | Steps 3-6, 8 |
| 12 | Build Projects views | Steps 3-6, 8 |
| 13 | Build Invoices views (Create — most complex) | Steps 3-6, 8 |
| 14 | Build Receipts views | Steps 3-6, 8 |
| 15 | Build Dashboard view + Chart.js partials | Steps 3, 6, 8 |
| 16 | Build Admin views (Trash, AuditLog, Users, Settings, FiscalYears) | Steps 3-6 |
| 17 | Build remaining views (Notifications, BusinessProfile, NotFound, Error) | Steps 3, 7 |
| 18 | Remove all Blazor files (Components/, Themes/, wwwroot/app.css, wwwroot/scripts.js) | Steps 9-17 complete |
| 19 | Build + fix compilation errors | All steps |
| 20 | E2E test all routes | Step 19 |

---

## File Count Summary

| Category | Added | Removed | Modified |
|----------|-------|---------|----------|
| MVC Views (`.cshtml`) | ~60 | 0 | 0 |
| Partial Views (`.cshtml`) | ~12 | 0 | 0 |
| Controllers (`.cs`) | ~15 | 0 | 0 |
| ViewModels (`.cs`) | ~5 | 0 | 0 |
| Helpers/Extensions | ~3 | 0 | 0 |
| `wwwroot/css/site.css` | 1 | 0 | 0 |
| `wwwroot/js/site.js` | 1 | 0 | 0 |
| `Program.cs` | 0 | 0 | 1 |
| `.csproj` | 0 | 0 | 1 |
| Blazor `.razor` files | 0 | ~89 | 0 |
| Blazor C# UI files | 0 | ~6 | 0 |
| MudBlazor/VercelTheme | 0 | ~1 | 0 |
| `wwwroot/` old files | 0 | ~3 | 0 |
| **Total** | **~97** | **~99** | **2** |

---

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **HTMX vs full JS** | HTMX for server interactions (tables, filters, search, pagination) | Minimal JS needed. Leverages existing MVC pipeline |
| **Alpine.js vs React/Vue** | Alpine.js for client-only state (dark mode, sidebar toggle, confirm dialogs, invoice form math) | Tiny (14KB), no build step, works inline in HTML |
| **Tailwind CDN vs build** | CDN for migration, build step before production | Faster migration, no npm dependency upfront |
| **Charts** | Chart.js (CDN) | Same as MudBlazor's underlying chart library, minimal migration |
| **Date picker** | Native `<input type="date">` | Sufficient for internal app. Flatpickr if more UX polish needed |
| **Icons** | Heroicons (inline SVG) | Free, Tailwind ecosystem, matches Inter font aesthetic |
| **Auth flow** | Unchanged (cookie-based Identity) | No auth migration needed. Same Program.cs Identity setup |
