# Changelog

## Codebase Audit (2026-07-02)

- **Fixed:** MudBlazor CSS 404 — switched from broken CDN to local `_content/MudBlazor/MudBlazor.min.css`
- **Fixed:** `blazor.web.js` 404 — Docker publish now includes `_framework` assets (restore with `-r linux-x64`, removed `--no-restore`/`PublishTrimmed`)
- **Fixed:** Audit log pagination — `GetRecentAsync` returns `PagedResult<AuditLog>` with real total count (was hardcoded `10000`)
- **Fixed:** `ConfirmEmailChange.razor` — string interpolation bug (literal `{userId}` shown)
- **Fixed:** N+1 query in `UserService.GetPagedAsync` — batched role lookups via `IdentityUserRole` join
- **Fixed:** N+1 in `OverdueBackgroundService` — batched audit log writes, single `SaveChangesAsync`
- **Fixed:** Unbounded overdue invoice query — added `.Take(500)` limit
- **Fixed:** Windows-only timezone ID — added Linux fallback (`Asia/Kolkata`)
- **Fixed:** Magic string setting keys — extracted to `SettingKeys` constants class
- **Fixed:** Hardcoded admin password in `SeedData.cs` — reads from `ADMIN_PASSWORD` env var
- **Fixed:** `OverdueBackgroundService` — moved `QuestPDF.Settings.License` to `Program.cs` startup
- **Fixed:** `Reports.razor` — integer division truncates percentages → `Math.Round`
- **Fixed:** `RecordPaymentDialog` — negative `Max` value → `Math.Max(0, ...)`
- **Fixed:** `ClientStatusCalculator` — added null guard, magic number → constant
- **Fixed:** `DeletePersonalData.razor` — redirects to `/` instead of deleted current page
- **Fixed:** `ResetPassword.razor` — invalid base64 on `Code` now handled with try-catch
- **Fixed:** `PasskeySubmit.razor.js` — removed `console.error` debug logs
- **Fixed:** Migration/seed in `Program.cs` — wrapped in try-catch with `Log.Error`
- **Fixed:** `.env.example` — changed password from `postgres` to `changeme`
- **Removed:** Empty `NavMenu.razor.css`, stray `agent-build-command (2).md`
- **Added:** `AUDIT_REPORT.md` — comprehensive code quality tracking document

## Phase 1 — Foundation (2026-07-02)

- Scaffolded solution with Clean Architecture (4 projects)
- Domain entities: ApplicationUser, Client, Project, Invoice, InvoiceItem, AuditLog
- Enums: ClientStatus, ProjectStatus, InvoiceStatus
- Constants: RoleNames, PolicyNames (no magic strings)
- Application interfaces & DTOs
- Infrastructure: ApplicationDbContext with Fluent API configuration, DependencyInjection
- SeedData for Admin/Staff roles + default admin user
- ASP.NET Core Identity with policy-based authorization
- MudBlazor 9.5 + VercelTheme (Material Design → Vercel-inspired)
- Serilog structured logging (console + file)
- Health check endpoint (`/health`)
- Multi-stage Dockerfile (Ubuntu Chiseled)
- Docker Compose (web + SQL Server 2022)
- EF Core initial migration

## Phase 2 — Client Management (2026-07-02)

- `ClientService` implementation with `IDbContextFactory<ApplicationDbContext>` (Infrastructure layer)
- `IClientService` registration in `DependencyInjection.cs`
- Client list page (`/clients`) with server-side MudTable — pagination, sorting, live search
- Traffic-light status chips (Green/Yellow/Red) via `ClientStatusCalculator`
- Client create page (`/clients/create`) with MudForm validation
- Client edit page (`/clients/edit/{id}`) with pre-populated form
- Client detail page (`/clients/{id}`) showing client info + related projects/invoices tables
- Delete confirmation via JS `confirm()` + Snackbar feedback
- Global usings expanded for `Domain.Enums`, `Application.Interfaces`, `Application.DTOs`, `Application.Services`

## Phase 3 — Project Management (2026-07-02)

- Extended `IProjectService` with `GetPagedAsync` for server-side pagination/sorting
- `ProjectService` implementation with `IDbContextFactory<ApplicationDbContext>`
- `IProjectService` registration in `DependencyInjection.cs`
- Project list page (`/projects`) with MudTable — pagination, sorting, live search, progress bars
- Project create page (`/projects/create`) with client selector dropdown + date picker
- Project edit page (`/projects/edit/{id}`) with status workflow selector + progress numeric input
- Project detail page (`/projects/{id}`) showing project info + linked client

## Phase 4 — Invoice Management (2026-07-02)

- Extended `IInvoiceService` with `GetPagedAsync`, `UpdateAsync`, `DeleteAsync`
- `InvoiceService` implementation with auto-numbering (`INV-{year}-{seq:0004}`)
- `InvoicePdfService` implementation with QuestPDF — professional A4 invoice template
- Service registrations in `DependencyInjection.cs`
- Invoice list page (`/invoices`) with MudTable — search by invoice # or client, status chips
- Invoice create page (`/invoices/create`) with dynamic line items table (add/remove rows, auto-calc totals)
- Invoice edit page (`/invoices/edit/{id}`) — editing gated to Draft status only
- Invoice detail page (`/invoices/{id}`) with status workflow buttons (Draft→Sent→Paid/Overdue) + PDF download
- Client status recalculation on invoice status change via `ClientStatusCalculator`
- JS `downloadFile` utility for PDF downloads
- Status workflow enforcement: Draft (editable) → Sent → Paid/Overdue (terminal)

## Phase 5 — Dashboard & Reports (2026-07-02)

- `DashboardStats` DTOs for aggregate metrics and status breakdowns
- `IDashboardService` interface with `GetStatsAsync()` + `GetRecentInvoicesAsync()`
- `DashboardService` implementation with efficient aggregate queries (counts, sums, groupings)
- Dashboard (`/`) — live stat cards (Clients, Active Projects, Overdue Invoices, Revenue)
- Dashboard — client/project status distribution with color-coded progress bars
- Dashboard — recent invoices table (last 5)
- Reports page (`/reports`) — Admin-only full stats, status breakdown tables, invoice summary

## Phase 6 — Audit Logging, Email & Overdue Background Service (2026-07-02)

- `AuditLogConfiguration` — Fluent API with indexes on Timestamp and (EntityName, EntityId)
- `IAuditService.LogAsync` + `GetRecentAsync` — write and query audit trail
- `AuditService` — writes audit logs via `IDbContextFactory`
- Audit logging wired into `ClientService`, `ProjectService`, `InvoiceService` (Created, Updated, Deleted, StatusChanged)
- Audit Log page (`/audit`) — Admin-only, server-side MudTable with pagination + color-coded action chips
- NavMenu — "Audit Log" link in admin-only section with History icon
- `IEmailService` + `EmailService` (MailKit SMTP, configurable via `IOptions<EmailSettings>`)
- `InvoiceEmailTemplate` — inline-CSS HTML template for overdue notifications
- `EmailSettings` + `OverdueSchedulerSettings` — strongly-typed configuration classes
- `OverdueBackgroundService` — `BackgroundService` runs Mon–Fri at configured hour:min, marks Sent→Overdue, logs audit, sends email
- Registered `OverdueBackgroundService` + settings binding in `Program.cs`
- `appsettings.json` and `.env.example` updated with Email + Scheduler config sections

## Phase 7 — Exports (Excel/CSV) (2026-07-02)

- `IExportService` + `ExportService` — ClosedXML workbook builder for Clients, Projects, Invoices, Reports
- Clients, Projects, Invoices list pages — "Export" button (Outlined variant) with loading state
- Reports page — "Export Report" button with multi-sheet workbook
- `scripts.js` — `downloadFile` updated to accept optional MIME type parameter

## Phase 8 — Payment Tracking (2026-07-02)

- `Payment` entity (Id, InvoiceId, Amount, PaymentDate, Method, Reference, Notes)
- `PaymentMethod` enum (Cash, Cheque, BankTransfer, Online)
- `PaymentConfiguration` — Fluent API with decimal precision, indexes
- Migration: `AddPaymentEntity`
- `IPaymentService` + `PaymentService` — record/delete payments, auto-mark invoice as Paid when fully paid, recalculate client status
- `PaymentService` — audit logging on record/delete
- `InvoiceService.GetByIdAsync` — includes `Payments` navigation property
- `Invoice` entity — added `ICollection<Payment> Payments`
- Invoice Detail page — payments table, balance due, Record Payment dialog
- `RecordPaymentDialog` — MudDialog with Amount, Method, Reference, Notes fields
- Manual "Mark as Paid"/"Mark as Overdue" buttons retained for non-payment workflows

## Phase 9 — User Management (2026-07-02)

- `UserDto` record for user list display
- `IUserService` + `UserService` — CRUD with `UserManager`/`RoleManager`, role assignment
- Users list page (`/users`) — Admin-only, MudTable with search, pagination, role chips
- Create User page (`/users/create`) — form with name, email, password, role toggle buttons
- Edit User page (`/users/edit/{id}`) — edit name/email, assign/detach roles
- NavMenu — "Users" link in admin-only section

## Phase 10 — Notification Preferences (2026-07-02)

- `NotificationPreference` entity (UserId, Type, EmailEnabled)
- Unique composite index on (UserId, Type)
- Migration: `AddNotificationPreference`
- `INotificationPreferenceService` + `NotificationPreferenceService` — get/toggle/seed defaults
- Notification Preferences page (`/account/notifications`) — toggle switches per type
- Default types: InvoiceOverdue, InvoicePaid, NewUserRegistered
- NavMenu — "Notifications" link

## Phase 11 — Configurable Background Service (DB-driven) (2026-07-02)

- `AppSetting` entity — simple key-value store (Key PK, max 500 chars)
- `AppSettingConfiguration` — Fluent API config
- Migration: `AddAppSetting`
- `IAppSettingService` + `AppSettingService` — get/set/getAll with `IDbContextFactory`
- Admin Scheduler Settings page (`/admin/settings`) — configure RunHour, RunMinute, TimeZone, Enabled
- `OverdueBackgroundService` refactored — reads scheduler config from DB via `IAppSettingService` instead of `IOptions`
- Removed `OverdueSchedulerSettings` binding from `Program.cs`
- NavMenu — "Scheduler Config" link in admin-only section

## Phase 12 — Soft Delete (2026-07-02)

- `IsDeleted`/`DeletedAt` added to Client, Project, Invoice entities
- Fluent API configs + index on `IsDeleted`
- Migration: `AddSoftDelete`
- All services filter `IsDeleted` in user-facing queries
- Cascade soft-delete on client (soft-deletes Projects and Invoices)
- New service methods: `GetDeletedPagedAsync`, `RestoreAsync`, `PermanentDeleteAsync`
- Trash page (`/trash`) — Admin-only, lists deleted clients/projects/invoices with restore/permanent-delete actions
- NavMenu — "Trash" link in admin-only section
- Delete confirmation messages updated to "moved to trash"

## Phase 13 — Advanced Filtering (2026-07-02)

- Filter DTOs: `ClientFilterDto`, `ProjectFilterDto`, `InvoiceFilterDto`
- Service `GetPagedAsync` signatures updated to accept filter DTOs
- Collapsible filter UI on Clients, Projects, Invoices list pages
- Multi-select status toggle buttons, MudDatePicker for date ranges, client dropdown
- Apply / Clear All buttons

## PostgreSQL Migration & Docker Optimization (2026-07-02)

- Switched from SQL Server 2022 to PostgreSQL 18
- Replaced `Microsoft.EntityFrameworkCore.SqlServer` with `Npgsql.EntityFrameworkCore.PostgreSQL 10.0.2`
- Updated `DependencyInjection.cs` — `UseNpgsql` replaces `UseSqlServer`
- Fixed `PaymentConfiguration.cs` — `.HasColumnType` → `.HasPrecision` for PostgreSQL
- Regenerated initial migration for PostgreSQL
- Updated connection strings in `appsettings.json` and `docker-compose.yml`
- Optimized Dockerfile: NuGet cache mount, fixed healthcheck, added resource limits
- Updated `.dockerignore`
