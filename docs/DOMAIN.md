# SunyaSuite Domain Model

## Business Domain

SunyaSuite is a business management platform for Nepali companies. It provides invoicing (VAT/PAN bill), project tracking, client management, money receipting, and fiscal year management with full multi-tenant support.

---

## Entity Glossary

### Config (shared infrastructure)

| Entity | File | Description |
|--------|------|-------------|
| `ApplicationUser` | `Domain/Entities/Config/ApplicationUser.cs` | ASP.NET Core Identity user with `FirstName`, `LastName`, `CreatedAt`, `DateDisplayPreference` |
| `Organization` | `Domain/Entities/Config/Organization.cs` | Tenant — top-level container. Has optional `ConnectionString` for separate DB. Soft-deletable via `DeletedAt`. |
| `OrganizationUser` | `Domain/Entities/Config/OrganizationUser.cs` | Join entity linking User ↔ Organization. Stores `Role`, `DefaultCompanyId`, `DefaultBranchId`. |
| `Invite` | `Domain/Entities/Config/Invite.cs` | Invitation code for invite-only registration. Has `Code`, `ExpiresAt`, `IsUsed`, `UsedByEmail`. |

### Tenant (business data, company-scoped)

| Entity | File | Description |
|--------|------|-------------|
| `Company` | `Domain/Entities/Tenant/Company.cs` | Business entity (legal entity). Has `Name`, `Slug`, `Email`, `Address`, `Phone`, `PanNumber`, `LogoBase64`. Root of tenant schema. Implements soft-delete. |
| `Branch` | `Domain/Entities/Tenant/Branch.cs` | Location/branch of a company. Scoped to `CompanyId`. Implements `ICompanyScoped`. Soft-deletable. |
| `Client` | `Domain/Entities/Tenant/Client.cs` | Customer/party. Has `Name`, `Company`, `Email`, `Phone`, `Address`, `PanNumber`, `Status` (Green/Yellow/Red). Scoped to `CompanyId`. Soft-deletable. |
| `FiscalYear` | `Domain/Entities/Tenant/FiscalYear.cs` | Accounting period. Stores `YearName` (e.g. "2081/82"), both BS and AD start/end dates. `IsOpen` controls whether new invoices can be created. `IsCurrent` marks the active year. |
| `Invoice` | `Domain/Entities/Tenant/Invoice.cs` | Core business document. Supports VAT bills and PAN (regular) bills. Has line items, buyer/seller info, auto-calculated totals, payment tracking, concurrency token. |
| `InvoiceItem` | `Domain/Entities/Tenant/InvoiceItem.cs` | Line item on an invoice. Has `Description`, `HsCode`, `Quantity`, `UnitPrice`, `Amount` (computed). Optional `ProjectId`. |
| `MoneyReceipt` | `Domain/Entities/Tenant/MoneyReceipt.cs` | Payment receipt. Has `ReceivedFromName`, `AmountReceived`, `PaymentMethod`, allocations to invoices. Concurrency token. |
| `ReceiptInvoiceAllocation` | `Domain/Entities/Tenant/ReceiptInvoiceAllocation.cs` | Join entity for receipt → invoice payment allocation. Tracks `AllocatedAmount`. |
| `Project` | `Domain/Entities/Tenant/Project.cs` | Tracked work item. Has `Name`, `Description`, `Deadline`, `ProgressPercent` (0-100), `Status`. Scoped to `Client`. Soft-deletable. |
| `NotificationPreference` | `Domain/Entities/Tenant/NotificationPreference.cs` | Per-user notification settings. Types: `InvoiceOverdue`, `InvoicePaid`, `NewUserRegistered`. |
| `AuditLog` | `Domain/Entities/Tenant/AuditLog.cs` | Immutable audit trail. Records `Action`, `EntityName`, `EntityId`, `UserId`, `Timestamp`, `Details`. |

---

## Business Rules (inferred from code)

### Invoice Rules

**Calculation** (`Invoice.Recalculate(vatRatePercentage = 13m)`):
```
Subtotal = SUM(Items.Amount)
VAT = BillType == VatBill && !IsAbbreviated ? Subtotal × vatRate ÷ 100 : 0
Total = Subtotal + VatAmount - DiscountAmount
```

**Status Transitions** (enforced in `InvoiceService`):
```
Draft → Sent
Sent → Overdue (via background service)
Sent → Paid (via receipt allocation)
```

**Payment Tracking:**
- `RecordPayment(decimal)` — adds to `AmountPaid` (positive only)
- `ReversePayment(decimal)` — subtracts from `AmountPaid` (positive only)
- `IsFullyPaid` → `AmountPaid >= Total`
- When fully paid via receipt allocation, status auto-changes to `Paid`

**Concurrency:** `RowVersion` byte[] — optimistic concurrency check on updates

### Client Status Rules

Calculated by `IClientStatusCalculator`:
- **Red**: Any active invoice is overdue (past due date, status=Sent or Overdue)
- **Yellow**: No overdue invoices, but at least one sent invoice due within 7 days
- **Green**: Otherwise (all paid or no active invoices)

### Project Rules

- `ProgressPercent` is clamped 0–100 in the property setter
- Soft-deleting a project cascades to related invoices (sets `ProjectId` to null on Invoice, or marks related invoices as deleted)

### Fiscal Year Rules

- Only one fiscal year can be `IsCurrent` at a time
- Closed fiscal years (`IsOpen = false`) should prevent new invoice/receipt creation
- Fiscal years are named after the Nepali calendar (e.g., "2081/82")

### Money Receipt Rules

- A receipt can allocate payment to multiple invoices
- Each allocation updates the invoice's `AmountPaid` and potentially its status
- **Each allocation amount must not exceed the invoice's remaining balance** (`Total - AmountPaid`). Enforced in `MoneyReceiptService.CreateAsync` and `MoneyReceiptService.UpdateAsync`.
- If an allocation fully covers the remaining balance, the invoice status auto-flips to `Paid`
- Soft-deleting a receipt reverses all allocations (subtracts from `AmountPaid`), reverting status to `Sent` if the invoice was `Paid` and is no longer fully paid
- Restoring a receipt re-applies allocations and re-flips status to `Paid` if fully covered

### Soft-Delete Rules

Applied consistently across `Client`, `Invoice`, `Project`, `MoneyReceipt`, `Company`, `Branch`, `Organization`:
- `IsDeleted = true` + `DeletedAt = DateTime.UtcNow`
- EF Core query filter excludes soft-deleted items from normal queries
- Restore sets `IsDeleted = false` and `DeletedAt = null`
- Permanent delete physically removes from database (used from trash UI)

---

## Enums

| Enum | Values | Used By |
|------|--------|---------|
| `BillType` | `VatBill`, `PanBill` | Invoice |
| `ClientStatus` | `Green`, `Yellow`, `Red` | Client |
| `CopyType` | `Original`, `Duplicate`, `Triplicate` | PDF generation |
| `DateDisplayPreference` | `Gregorian = 0`, `Nepali = 1` | ApplicationUser |
| `InvoiceStatus` | `Draft`, `Sent`, `Paid`, `Overdue` | Invoice |
| `PaymentMethod` | `Cash`, `Cheque`, `BankTransfer`, `Online`, `Card`, `QR` | MoneyReceipt |
| `ProjectStatus` | `NotStarted`, `InProgress`, `Completed`, `OnHold` | Project |
| `OrgRoles` (static class) | `Owner`, `OrgAdmin`, `Member`, `Viewer` | OrganizationUser |

---

## Domain Interfaces

| Interface | Purpose |
|-----------|---------|
| `ICompanyScoped` | Marker interface for entities that belong to a company. Exposes `Guid CompanyId`. Used by EF Core to auto-filter and auto-assign company scoping. |

---

## Business Logic Location

| Logic | Location | Type |
|-------|----------|------|
| Invoice calculation | `Invoice.Recalculate()` | Domain entity method |
| Payment tracking | `Invoice.RecordPayment()`, `ReversePayment()` | Domain entity methods |
| Payment status | `Invoice.IsFullyPaid` | Domain computed property |
| Progress clamping | `Project.ProgressPercent` setter | Domain property logic |
| Invoice amount | `InvoiceItem.Amount` | Domain computed property |
| Status transitions | `InvoiceService` | Application service |
| Client status calculation | `ClientStatusCalculator` | Application service |
| Overdue detection | `OverdueBackgroundService` | Infrastructure background service |
| Receipt allocation | `MoneyReceiptService` | Application service |
| Invite expiration | `InviteDto.IsExpiredAsOf()` | Application DTO method |

> **Note**: Most entities are anemic (pure data with no behavior). Core business logic lives in `Invoice`, `Project`, and `InvoiceItem` entity methods. Complex transaction logic (status transitions, payment allocations) lives in service implementations.
