# Codebase Audit Report

Generated: 2026-07-02

## Summary

| Category | Score |
|----------|-------|
| Security | 7/10 |
| Code Quality | 8/10 |
| Scalability | 6/10 |
| Overall | 7/10 |

---

## Fixed Issues

### 404 Resources (P0)

| File | Issue | Fix |
|------|-------|-----|
| `Components/App.razor:9` | MudBlazor CSS CDN 404 — `cdn.jsdelivr.net` path broken | Changed to local `_content/MudBlazor/MudBlazor.min.css` |
| `Components/App.razor:23` | `_framework/blazor.web.js` 404 — missing from Docker publish | Added `-r linux-x64` to restore + removed `--no-restore` in Dockerfile |
| `Dockerfile` | `PublishTrimmed=true` removed blazor framework assets | Removed `PublishTrimmed` (image still ~170MB) |

### P1 — Critical Bugs

| File | Issue | Fix |
|------|-------|-----|
| `AuditService.cs` | `GetRecentAsync` didn't return total count | Changed return to `PagedResult<AuditLog>` with real count |
| `AuditLog/Index.razor:62` | `_totalItems = 10000` hardcoded — pagination broken | Using actual total from service |
| `ConfirmEmailChange.razor:43` | String interpolation bug — literal `{userId}` shown | Changed to "Unable to find user." |
| `UserService.cs:42-46` | N+1 — `GetRolesAsync` per user | Batch query via `IdentityUserRole` join |
| `OverdueBackgroundService.cs:88-107` | N+1 — `auditService.LogAsync` per invoice in loop | Batched audit logs + single `SaveChangesAsync` |
| `OverdueBackgroundService.cs:83-86` | Unbounded overdue query (no limit) | Added `.Take(500)` |
| `OverdueBackgroundService.cs:41` | Windows-only timezone ID ("India Standard Time") | Added Linux fallback ("Asia/Kolkata") |
| `OverdueBackgroundService.cs:38-41` | Magic string setting keys | Extracted to `SettingKeys` constants class |
| `SeedData.cs:38` | Hardcoded admin password | Read from `ADMIN_PASSWORD` env var with `"Admin@123"` fallback |
| `DeletePersonalData.razor:87` | Redirects to deleted page after account deletion | Changed to `RedirectTo("/")` |
| `ResetPassword.razor:64` | Invalid base64 on `Code` throws unhandled exception | Added try-catch with redirect |

### P2 — Medium Bugs

| File | Issue | Fix |
|------|-------|-----|
| `Reports.razor:68,91` | Integer division truncates percentages | Changed to `Math.Round((double)... * 100)` |
| `RecordPaymentDialog.razor:13` | `Max` could be negative | `Math.Max(0, InvoiceTotal - PaidAmount)` |
| `ClientStatusCalculator.cs:8` | Missing null guard on `invoices` parameter | Added `ArgumentNullException.ThrowIfNull` |
| `ClientStatusCalculator.cs:18` | Magic number `7` | Extracted as `YellowStatusThresholdDays` constant |
| `InvoicePdfService.cs:16` | `QuestPDF.Settings.License` set per-call | Moved to `Program.cs` startup |
| `Program.cs:92-99` | Migration/seed could throw without context | Added try-catch with `Log.Error` |
| `PasskeySubmit.razor.js:14,103` | `console.error` debug logs | Removed |
| `NavMenu.razor.css` | Empty file | Deleted |
| `agent-build-command (2).md` | Stray duplicate file at root | Deleted |
| `.env.example:1` | Contains real dev password `postgres` | Changed to `changeme` |

---

## Remaining Issues (Post-MVP)

### Security

| Issue | File | Detail |
|-------|------|--------|
| Hardcoded DB password in appsettings.json | `src/SunyaSuite.Web/appsettings.json:3` | Dev password `postgres` for local dev; Docker override via env var is correct. Consider using User Secrets or Key Vault for production. |
| Authenticator key in personal data export | `IdentityComponentsEndpointRouteBuilderExtensions.cs:143` | `GetAuthenticatorKeyAsync` result included in download. Low risk (user sees own key). |

### Scalability / Performance

| Issue | File | Detail |
|-------|------|--------|
| `ToLower().Contains()` prevents index usage | `ClientService.cs`, `InvoiceService.cs`, `ProjectService.cs` | Should use `EF.Functions.ILike()` for PostgreSQL case-insensitive search |
| All client invoices loaded for status calc | `ClientService.cs:142-144`, `InvoiceService.cs:191-207`, `PaymentService.cs:42-46` | Should use `AnyAsync()`/aggregate query instead of loading all invoices |
| Race condition in invoice number generation | `InvoiceService.cs:315-332` | Should use DB sequence (`NpgsqlSequence`) or serialized operation |
| Unbounded export queries | `ExportService.cs` | 3 export methods load all rows — add pagination or streaming |
| Double query pattern (Count + Skip/Take) | Multiple services | Single query with `COUNT(*) OVER()` could reduce roundtrips |
| Long functions >50 lines | `ExportService.cs` (87 lines), multiple `GetPagedAsync` methods | Extract query building and mapping logic |

### Missing Database Indexes

| Entity | Missing Indexes |
|--------|----------------|
| `Invoice` | `IssueDate`, `DueDate`, `Status`, composite `(Status, DueDate)` |
| `Client` | `Status`, `PaymentDueDate`, `RegisteredOn` |
| `Project` | `Deadline`, `Status`, composite `(ClientId, Deadline)` |
| `Payment` | `PaymentDate` |

### Architecture / Design

| Issue | Detail |
|-------|--------|
| Anemic domain model | All entities are POCOs with no behavior (design choice for CRUD) |
| Audit fields inconsistent | Some entities have `CreatedAt`/`CreatedBy`, others don't |
| Soft-delete inconsistency | `Payment` has no `IsDeleted` (likely intentional) |
| `UserService` uses `UserManager`/`RoleManager` directly | Bypasses generic service pattern — acceptable for Identity |

### Low Priority

| Issue | File |
|-------|------|
| Empty `NavMenu.razor.css` | Deleted |
| `RegisterConfirmation.razor` debug block | Shows confirmation link in dev (expected with `IdentityNoOpEmailSender`) |
| `ExternalLoginPicker.razor` uses Bootstrap classes | Should use MudBlazor for consistency |
| `AuditLog/Index.razor` — `StateHasChanged()` in `ServerReload` | Could cause re-render cycles (minor) |

---

## Files Changed During Audit

```
AUDIT_REPORT.md                          (new)
.env.example                             (password → changeme)
Dockerfile                               (restore -r linux-x64, removed --no-restore, removed PublishTrimmed)
src/SunyaSuite.Application/Interfaces/IAuditService.cs
src/SunyaSuite.Application/Services/ClientStatusCalculator.cs
src/SunyaSuite.Infrastructure/DataSeeding/SeedData.cs
src/SunyaSuite.Infrastructure/Services/AuditService.cs
src/SunyaSuite.Infrastructure/Services/InvoicePdfService.cs
src/SunyaSuite.Infrastructure/Services/OverdueBackgroundService.cs
src/SunyaSuite.Infrastructure/Services/UserService.cs
src/SunyaSuite.Web/Components/Account/Pages/ConfirmEmailChange.razor
src/SunyaSuite.Web/Components/Account/Pages/Manage/DeletePersonalData.razor
src/SunyaSuite.Web/Components/Account/Pages/ResetPassword.razor
src/SunyaSuite.Web/Components/Account/Shared/PasskeySubmit.razor.js
src/SunyaSuite.Web/Components/App.razor
src/SunyaSuite.Web/Components/Layout/NavMenu.razor.css        (deleted)
src/SunyaSuite.Web/Components/Pages/AuditLog/Index.razor
src/SunyaSuite.Web/Components/Pages/Invoices/RecordPaymentDialog.razor
src/SunyaSuite.Web/Components/Pages/Reports.razor
src/SunyaSuite.Web/Program.cs
agent-build-command (2).md                                     (deleted)
```

## Verification

- `http://localhost:8080/_framework/blazor.web.js` → **200**
- `http://localhost:8080/_content/MudBlazor/MudBlazor.min.css` → **200**
- `http://localhost:8080` → **302** (redirect to login)
- Docker build: ✅ passes
