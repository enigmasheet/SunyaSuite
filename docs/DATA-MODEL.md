# SunyaSuite Data Model

## Database Architecture

Two PostgreSQL databases:

| Database | Connection String Key | DbContext | Purpose |
|----------|----------------------|-----------|---------|
| **Config DB** | `ConfigConnection` | `ConfigDbContext` | Shared — Organizations, Identity/Auth, Invites |
| **Tenant DB** | `TemplateConnection` | `ApplicationDbContext` | Per-tenant — Companies, Clients, Invoices, Projects, Receipts |

Organizations with `ConnectionString != null` get their own **separate Tenant database instance**. All others share the template connection.

---

## Config DB Schema

### Tables

#### `AspNetUsers` (Identity)
| Column | Type | Notes |
|--------|------|-------|
| Id | TEXT (PK) | IdentityUser inherited |
| UserName, Email, PasswordHash, etc. | TEXT | Standard Identity fields |
| FirstName | TEXT (max 100) | Custom |
| LastName | TEXT (max 100) | Custom |
| CreatedAt | TIMESTAMP | Auto-set |
| Preference | INT | `DateDisplayPreference` enum (0=Gregorian, 1=Nepali) |

Plus all standard ASP.NET Identity tables: `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`.

#### `Organizations`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| Name | VARCHAR(200) | Not null |
| Slug | VARCHAR(100) | **Unique index**, not null |
| ConnectionString | VARCHAR(500) | Nullable — separate DB support |
| IsActive | BOOLEAN | Default true |
| CreatedAt | TIMESTAMP | Auto-set |
| DeletedAt | TIMESTAMP | Nullable — soft-delete |
| Query filter | `WHERE DeletedAt IS NULL` | |

#### `OrganizationUsers`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| OrganizationId | UUID (FK → Organizations) | Cascade delete |
| UserId | VARCHAR(100) (FK → AspNetUsers.Id) | Cascade delete |
| Role | VARCHAR(50) | Owner, OrgAdmin, Member, Viewer |
| JoinedAt | TIMESTAMP | Auto-set |
| DefaultCompanyId | UUID | Nullable |
| DefaultBranchId | UUID | Nullable |
| Unique index | `(OrganizationId, UserId)` | |

#### `Invites`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| OrganizationId | UUID | Not null |
| CompanyId | UUID | Nullable |
| Code | VARCHAR(200) | Not null |
| Role | VARCHAR(50) | Not null |
| IsUsed | BOOLEAN | Default false |
| CreatedAt | TIMESTAMP | |
| ExpiresAt | TIMESTAMP | |
| UsedByEmail | VARCHAR(256) | Nullable |
| UsedAt | TIMESTAMP | Nullable |

---

## Tenant DB Schema

### Tables

#### `Companies`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| Name | VARCHAR(200) | Not null |
| Slug | VARCHAR(100) | **Unique index** |
| Email | VARCHAR(200) | |
| Address | VARCHAR(500) | |
| Phone | VARCHAR(50) | |
| PanNumber | VARCHAR(15) | |
| IsActive | BOOLEAN | Default true |
| CreatedAt | TIMESTAMP | |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| LogoBase64 | TEXT | Nullable, company logo |
| Query filter | `WHERE IsDeleted = FALSE` | |
| Navigation | → `Branches` (HasMany, Cascade) | |

#### `Branches`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies) | `ICompanyScoped` |
| Name | VARCHAR(200) | |
| Slug | VARCHAR(100) | |
| Address | VARCHAR(500) | |
| Phone | VARCHAR(50) | |
| IsActive | BOOLEAN | Default true |
| CreatedAt | TIMESTAMP | |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| Unique index | `(CompanyId, Slug)` | |
| Query filter | `WHERE IsDeleted = FALSE` | |

#### `Clients`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| BranchId | UUID (FK → Branches, Restrict) | Nullable |
| Name | VARCHAR(200) | Not null |
| Company | VARCHAR(200) | Client's organization name |
| Email | VARCHAR(200) | **Unique index** |
| Phone | VARCHAR(50) | |
| Address | VARCHAR(500) | |
| PanNumber | VARCHAR(15) | Nullable |
| RegisteredOn | DATE | |
| Status | VARCHAR(20) | Green, Yellow, Red |
| CreatedAt | TIMESTAMP | |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| Indexes | `Name`, `IsDeleted`, `CompanyId` | |
| Query filter | `WHERE IsDeleted = FALSE` | |
| Navigations | → `Projects` (HasMany, Restrict), → `Invoices` (HasMany, Restrict) | |

#### `Projects`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| BranchId | UUID (FK → Branches, Restrict) | Nullable |
| ClientId | UUID (FK → Clients, Restrict) | |
| Name | VARCHAR(200) | |
| Description | VARCHAR(2000) | |
| Deadline | DATE | |
| ProgressPercent | INT | 0–100 |
| Status | VARCHAR(20) | NotStarted, InProgress, Completed, OnHold |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| Indexes | `IsDeleted`, `CompanyId`, `ClientId`, composite `(CompanyId, IsDeleted)` | |
| Query filter | `WHERE IsDeleted = FALSE` | |
| Navigation | → `Invoices` (HasMany) | |

#### `FiscalYears`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| YearName | VARCHAR(10) | **Unique index** (per company) |
| StartDateBS | VARCHAR(15) | Nepali date string |
| EndDateBS | VARCHAR(15) | Nepali date string |
| StartDateAD | DATE | Gregorian |
| EndDateAD | DATE | Gregorian |
| IsOpen | BOOLEAN | Default true |
| IsCurrent | BOOLEAN | Only one true per company |
| CreatedAt | TIMESTAMP | |
| Navigations | → `Invoices` (HasMany), → `MoneyReceipts` (HasMany) | |

#### `Invoices`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| BranchId | UUID (FK → Branches, Restrict) | Nullable |
| ClientId | UUID (FK → Clients, Restrict) | |
| FiscalYearId | UUID (FK → FiscalYears) | |
| ProjectId | UUID (FK → Projects) | Nullable |
| BillType | INT | Enum → `VatBill(0)`, `PanBill(1)` |
| InvoiceNumber | VARCHAR(50) | **Unique index**, auto-generated |
| IssueDate | DATE | |
| DueDate | DATE | |
| DateBS | VARCHAR(15) | Nepali date |
| Subtotal | DECIMAL(18,2) | |
| TaxRate | DECIMAL(5,2) | |
| DiscountAmount | DECIMAL(18,2) | |
| VatAmount | DECIMAL(18,2) | |
| Total | DECIMAL(18,2) | Private setter |
| GrandTotalInWords | VARCHAR(300) | |
| IsAbbreviated | BOOLEAN | |
| Status | VARCHAR(20) | Draft, Sent, Paid, Overdue |
| BuyerPan | VARCHAR(15) | |
| BuyerAddress | VARCHAR(500) | |
| SellerName | VARCHAR(200) | |
| SellerPan | VARCHAR(15) | |
| SellerAddress | VARCHAR(500) | |
| SellerPhone | VARCHAR(50) | |
| AmountPaid | DECIMAL(18,2) | Private setter |
| ProjectRemark | VARCHAR(500) | Nullable |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| SellerLogoBase64 | TEXT | Nullable |
| RowVersion | BYTEA | **Concurrency token** |
| Query filter | `WHERE IsDeleted = FALSE` | |
| Indexes | `InvoiceNumber` (unique), `ClientId`, `CompanyId`, `FiscalYearId`, `DueDate`, `Status`, composite `(CompanyId, IsDeleted)` | |
| Navigations | → `Items` (HasMany, Cascade), → `ReceiptAllocations` (HasMany, Restrict) | |

#### `InvoiceItems`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| InvoiceId | UUID (FK → Invoices, Cascade) | |
| LineNo | INT | |
| Description | VARCHAR(500) | |
| HsCode | VARCHAR(10) | Nullable — Harmonized System code |
| Unit | VARCHAR(20) | |
| Quantity | DECIMAL(18,2) | |
| UnitPrice | DECIMAL(18,2) | |
| ProjectId | UUID (FK → Projects) | Nullable |
| *Computed* | `Amount = Quantity * UnitPrice` | In-memory only, not stored |

#### `MoneyReceipts`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| BranchId | UUID (FK → Branches, Restrict) | Nullable |
| FiscalYearId | UUID (FK → FiscalYears) | |
| ReceiptNumber | VARCHAR(50) | **Unique index**, auto-generated |
| DateAD | DATE | |
| DateBS | VARCHAR(15) | Nepali date |
| ReceivedFromName | VARCHAR(200) | |
| ReceivedFromPan | VARCHAR(15) | Nullable |
| ReceivedFromAddress | VARCHAR(500) | Nullable |
| AmountReceived | DECIMAL(18,2) | |
| AmountInWords | VARCHAR(300) | |
| PaymentMethod | VARCHAR(20) | Cash, Cheque, BankTransfer, Online, Card, QR |
| ReferenceNo | VARCHAR(100) | Nullable |
| ReceivedBy | VARCHAR(200) | |
| IsDeleted | BOOLEAN | Default false |
| DeletedAt | TIMESTAMP | Nullable |
| SellerLogoBase64 | TEXT | Nullable |
| RowVersion | BYTEA | **Concurrency token** |
| Query filter | `WHERE IsDeleted = FALSE` | |
| Navigations | → `Allocations` (HasMany, Cascade) | |

#### `ReceiptInvoiceAllocations`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| MoneyReceiptId | UUID (FK → MoneyReceipts, Cascade) | |
| InvoiceId | UUID (FK → Invoices, Restrict) | |
| AllocatedAmount | DECIMAL(18,2) | |
| Indexes | `MoneyReceiptId`, `InvoiceId` | |

#### `AuditLogs`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| UserId | VARCHAR(100) | |
| Action | VARCHAR(50) | Created, Updated, Deleted, etc. |
| EntityName | VARCHAR(100) | Client, Invoice, etc. |
| EntityId | VARCHAR(100) | GUID as string |
| Timestamp | TIMESTAMP | |
| Details | VARCHAR(2000) | JSON or text |
| Indexes | `CompanyId`, `Timestamp`, `(EntityName, EntityId)` | |

#### `NotificationPreferences`
| Column | Type | Constraints |
|--------|------|-------------|
| Id | UUID (PK) | |
| CompanyId | UUID (FK → Companies, Restrict) | `ICompanyScoped` |
| UserId | VARCHAR(100) | |
| Type | VARCHAR(50) | InvoiceOverdue, InvoicePaid, NewUserRegistered |
| EmailEnabled | BOOLEAN | Default true |
| Unique index | `(UserId, Type)` | |

---

## Sequences

PostgreSQL sequences used for auto-numbering:
- `InvoiceSequence_{FiscalYearName}` — per-fiscal-year invoice number sequence
- `MR_Sequence_{FiscalYearName}` — per-fiscal-year money receipt sequence

---

## Migration Workflow

1. **Design-time factories** exist for both DbContexts (`ConfigDbContextFactory`, `ApplicationDbContextFactory`) with hardcoded local connection strings
2. Migrations are applied at **application startup** via `RunDatabaseStartupAsync()`:
   - `configCtx.Database.MigrateAsync()` — Config DB
   - `tenantCtx.Database.MigrateAsync()` — template Tenant DB
   - `ApplyTenantMigrationsService` — per-org separate databases
3. `DatabaseResetService` can drop+recreate both schemas and re-seed

### Migration History

**Config DB**: 3 migrations (initial, soft-delete on Organization, Invites moved from Tenant to Config)
**Tenant DB**: 12 migrations (initial schema → date refactoring → scoping → soft-delete → concurrency → final cleanup)

---

## EF Core Mapping Approach

- **Fluent API** exclusively (no data annotations on entities)
- Each entity has a dedicated `IEntityTypeConfiguration<T>` class in `Configurations/`
- Applied via `ApplyConfigurationsFromAssembly()` in `OnModelCreating`
- `MaxLength` set on all string properties
- `HasPrecision` for decimal properties
- `HasConversion` for enum-to-string or enum-to-int where appropriate
- `IsRequired` for non-nullable FKs
- `HasQueryFilter` for soft-delete on all tenant entities
- `IsConcurrencyToken` for `RowVersion` byte arrays
- Navigation properties use specific delete behaviors (Restrict, Cascade) rather than defaults

---

## Company Scoping

All tenant entities (except `Company` itself) implement `ICompanyScoped` with `Guid CompanyId`. The `ApplicationDbContext`:
- Auto-assigns `CompanyId` on newly added entities
- Prevents modification of `CompanyId` on existing entities
- The `ForCompany<T>()` extension method adds `.Where(e => e.CompanyId == currentCompanyId)` to queries
