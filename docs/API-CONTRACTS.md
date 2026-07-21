# SunyaSuite API Contracts

## Base URL

- Development: `http://localhost:5000`
- All routes are prefixed with `/api`

---

## Authentication

**Scheme**: JWT Bearer token  
**Header**: `Authorization: Bearer {token}`  
**Tenant**: `X-Tenant-ID: {organization-slug}` (required for tenant-scoped endpoints)

### Login → Token

```json
POST /api/auth/login
{
  "email": "string",
  "password": "string",
  "rememberMe": bool
}
→ 200
{
  "token": "string (JWT)",
  "expiresAt": "datetime",
  "userId": "string",
  "email": "string",
  "roles": ["string"],
  "organizations": [{ "id": "guid", "name": "string", "slug": "string", "hasSeparateDatabase": bool, "role": "string" }]
}
```

### Token Renewal

```json
POST /api/auth/renew
{ "token": "string" }
→ 200 (same shape as login response)
```

### Registration (invite-only)

```json
POST /api/auth/register
{
  "email": "string",
  "password": "string",
  "name": "string?",
  "inviteCode": "string"
}
→ 200 { "message": "Account created successfully. You can now sign in." }
```

---

## JWT Claims

| Claim | Type | Description |
|-------|------|-------------|
| `sub` (NameIdentifier) | string | User ID |
| `email` | string | User email |
| `name` | string | Username or email |
| `role` | string[] | ASP.NET Identity roles (SystemAdmin, etc.) |
| `org_role` | string[] | `{orgId}:{role}` — org-level roles for each org |
| `exp` | number | Expiration timestamp |

---

## Authorization Policies (on endpoints)

| Attribute | Access |
|-----------|--------|
| `[Authorize]` | Any authenticated user |
| `[Authorize(Policy = PolicyNames.SystemAdminOnly)]` | SystemAdmin role only |
| `[Authorize(Policy = PolicyNames.OrgAdminOrAbove)]` | Owner, OrgAdmin |
| `[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]` | Owner, OrgAdmin, Member |
| `[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]` | Owner, OrgAdmin, Member, Viewer |

---

## Config Endpoints (shared DB)

### Auth

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/login` | Anonymous | Login, returns JWT + orgs |
| POST | `/api/auth/register` | Anonymous | Invite-only registration |
| POST | `/api/auth/forgot-password` | Anonymous | Request password reset |
| POST | `/api/auth/change-password` | Authorized | Change password |
| POST | `/api/auth/renew` | Anonymous | Renew expired token |

### Users (SystemAdminOnly)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/users?page=&pageSize=&searchTerm=` | Paged user list |
| GET | `/api/users/roles` | List all Identity roles |
| GET | `/api/users/org-roles` | List org role names |
| GET | `/api/users/{id}` | Get user by ID |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| DELETE | `/api/users/{id}` | Delete user |
| POST | `/api/users/{id}/roles` | Assign roles to user |
| POST | `/api/users/roles` | Create new Identity role |
| DELETE | `/api/users/roles/{roleName}` | Delete Identity role |

### Organizations

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/organizations/my` | Authorized | Current user's organizations |
| GET | `/api/organizations` | SystemAdminOnly | Paged list of all orgs |
| POST | `/api/organizations` | SystemAdminOnly | Create org (optionally with separate DB) |
| GET | `/api/organizations/{orgId}` | SystemAdminOnly | Get org by ID |
| PUT | `/api/organizations/{orgId}` | SystemAdminOnly | Update org |
| DELETE | `/api/organizations/{orgId}` | SystemAdminOnly | Soft-delete org |
| GET | `/api/organizations/deleted` | SystemAdminOnly | List soft-deleted orgs |
| PATCH | `/api/organizations/{orgId}/restore` | SystemAdminOnly | Restore org |
| PATCH | `/api/organizations/{orgId}/toggle-active` | SystemAdminOnly | Toggle active state |
| GET | `/api/organizations/{orgId}/users` | OrgAdminOrAbove | List org users |
| POST | `/api/organizations/{orgId}/users` | OrgAdminOrAbove | Create user within org |
| POST | `/api/organizations/users/{userId}/orgs` | OrgAdminOrAbove | Assign user to org |
| PUT | `/api/organizations/users/{userId}/orgs/{orgId}` | OrgAdminOrAbove | Update user's org role |
| DELETE | `/api/organizations/users/{userId}/orgs/{orgId}` | OrgAdminOrAbove | Remove user from org |
| PUT | `/api/organizations/{orgId}/users/{userId}/defaults` | OrgAdminOrAbove | Set default company/branch |
| PUT | `/api/organizations/{orgId}/users/{userId}/role` | OrgAdminOrAbove | Change user's org role |

### Invites (OrgAdminOrAbove)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/invites?organizationId=&page=&pageSize=` | Paged invites list |
| POST | `/api/invites` | Create invite |
| DELETE | `/api/invites/{id}` | Revoke/delete invite |

### System Dashboard (SystemAdminOnly)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/system-dashboard` | System-wide stats (orgs, users) |

### Menu

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/auth/menu` | Authorized | Dynamic navigation menu sections |

### User Preferences (Authorized)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/user-preferences/date-display` | Get date preference |
| PUT | `/api/user-preferences/date-display` | Set date preference |

### Notification Preferences (Authorized)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/notification-preferences` | List user's preferences |
| PUT | `/api/notification-preferences/{type}` | Toggle preference |

### Audit Log (OrgAdminOrAbove)

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/audit?page=&pageSize=&searchTerm=&action=&entityName=&dateFrom=&dateTo=` | Paged audit log |
| GET | `/api/audit/distinct-actions` | List distinct actions |
| GET | `/api/audit/distinct-entities` | List distinct entity names |

---

## Tenant Endpoints (company-scoped, require X-Tenant-ID)

### Dashboard

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/dashboard?fiscalYearId=` | OrgViewerOrAbove | Company dashboard stats |
| GET | `/api/dashboard/recent-invoices?count=5` | OrgViewerOrAbove | Recent invoices |

### Companies

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/companies` | OrgMemberOrAbove | List all companies |
| GET | `/api/companies/active` | OrgMemberOrAbove | List active companies |
| GET | `/api/companies/deleted` | OrgAdminOrAbove | List deleted companies |
| GET | `/api/companies/{id}` | OrgMemberOrAbove | Get company detail |
| POST | `/api/companies` | OrgAdminOrAbove | Create company |
| PUT | `/api/companies/{id}` | OrgAdminOrAbove | Update company |
| DELETE | `/api/companies/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/companies/{id}/restore` | OrgAdminOrAbove | Restore |

### Branches

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/branches` | OrgMemberOrAbove | List all branches |
| GET | `/api/branches/deleted` | OrgAdminOrAbove | List deleted |
| GET | `/api/branches/company/{companyId}` | OrgMemberOrAbove | By company |
| GET | `/api/branches/{id}` | OrgMemberOrAbove | Detail |
| POST | `/api/branches` | OrgAdminOrAbove | Create |
| PUT | `/api/branches/{id}` | OrgAdminOrAbove | Update |
| DELETE | `/api/branches/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/branches/{id}/restore` | OrgAdminOrAbove | Restore |

### Clients

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/clients/options` | OrgMemberOrAbove | Dropdown list (id, name, pan) |
| GET | `/api/clients?page=&pageSize=&search=&sortLabel=&sortDirection=&statuses=&registeredOnFrom=&registeredOnTo=` | OrgViewerOrAbove | Paged list |
| GET | `/api/clients/deleted?page=&pageSize=&search=` | OrgAdminOrAbove | Deleted list |
| GET | `/api/clients/{id}` | OrgViewerOrAbove | Detail (with projects + invoices) |
| POST | `/api/clients` | OrgMemberOrAbove | Create |
| PUT | `/api/clients/{id}` | OrgMemberOrAbove | Update |
| DELETE | `/api/clients/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/clients/{id}/restore` | OrgAdminOrAbove | Restore |
| DELETE | `/api/clients/{id}/permanent` | SystemAdminOnly | Hard delete |

### Projects

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/projects/options?clientId=` | OrgMemberOrAbove | Dropdown list |
| GET | `/api/projects?page=&pageSize=&search=&sortLabel=&sortDirection=&statuses=&deadlineFrom=&deadlineTo=&clientId=` | OrgViewerOrAbove | Paged list |
| GET | `/api/projects/deleted?page=&pageSize=&search=` | OrgAdminOrAbove | Deleted list |
| GET | `/api/projects/{id}` | OrgViewerOrAbove | Detail |
| POST | `/api/projects` | OrgMemberOrAbove | Create |
| PUT | `/api/projects/{id}` | OrgMemberOrAbove | Update |
| DELETE | `/api/projects/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/projects/{id}/restore` | OrgAdminOrAbove | Restore |
| DELETE | `/api/projects/{id}/permanent` | SystemAdminOnly | Hard delete |

### Invoices

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/invoices/selection?fiscalYearId=` | OrgMemberOrAbove | Invoice selection list |
| GET | `/api/invoices?page=&pageSize=&search=&sortLabel=&sortDirection=&statuses=&issueDateFrom=&issueDateTo=&dueDateFrom=&dueDateTo=&clientId=&fiscalYearId=` | OrgViewerOrAbove | Paged list |
| GET | `/api/invoices/deleted?page=&pageSize=&search=` | OrgAdminOrAbove | Deleted list |
| GET | `/api/invoices/{id}` | OrgViewerOrAbove | Detail (with items + receipts) |
| POST | `/api/invoices` | OrgMemberOrAbove | Create |
| PUT | `/api/invoices/{id}` | OrgMemberOrAbove | Update |
| PATCH | `/api/invoices/{id}/status` | OrgMemberOrAbove | Update status |
| DELETE | `/api/invoices/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/invoices/{id}/restore` | OrgAdminOrAbove | Restore |
| DELETE | `/api/invoices/{id}/permanent` | SystemAdminOnly | Hard delete |

### Money Receipts

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/money-receipts?page=&pageSize=&search=&sortLabel=&sortDirection=&fiscalYearId=` | OrgViewerOrAbove | Paged list |
| GET | `/api/money-receipts/deleted?page=&pageSize=&search=` | OrgAdminOrAbove | Deleted list |
| GET | `/api/money-receipts/{id}` | OrgViewerOrAbove | Detail |
| POST | `/api/money-receipts` | OrgMemberOrAbove | Create |
| PUT | `/api/money-receipts/{id}` | OrgMemberOrAbove | Update |
| DELETE | `/api/money-receipts/{id}` | OrgAdminOrAbove | Soft-delete |
| PATCH | `/api/money-receipts/{id}/restore` | OrgAdminOrAbove | Restore |
| DELETE | `/api/money-receipts/{id}/permanent` | SystemAdminOnly | Hard delete |

### Fiscal Years

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/fiscal-years` | OrgViewerOrAbove | List all |
| GET | `/api/fiscal-years/current` | OrgViewerOrAbove | Get current FY |
| GET | `/api/fiscal-years/open` | OrgViewerOrAbove | List open FYs |
| GET | `/api/fiscal-years/{id}` | OrgViewerOrAbove | Detail |
| POST | `/api/fiscal-years` | OrgAdminOrAbove | Create |
| PATCH | `/api/fiscal-years/{id}/toggle-open` | OrgAdminOrAbove | Toggle open/closed |
| PATCH | `/api/fiscal-years/{id}/set-current` | OrgAdminOrAbove | Set as current |

### PDF Generation

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/invoice-pdf/{id}?copyType=&preference=` | OrgMemberOrAbove | Download invoice PDF |
| GET | `/api/receipt-pdf/{id}?copyType=&preference=` | OrgMemberOrAbove | Download receipt PDF |

### Email

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/email/invoice/{id}` | OrgMemberOrAbove | Send invoice as email with PDF |
| POST | `/api/email/money-receipt/{id}` | OrgMemberOrAbove | Send receipt as email with PDF |

### Export (Excel via ClosedXML)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| GET | `/api/export/clients` | OrgAdminOrAbove | Export clients to Excel |
| GET | `/api/export/projects` | OrgAdminOrAbove | Export projects to Excel |
| GET | `/api/export/invoices` | OrgAdminOrAbove | Export invoices to Excel |
| GET | `/api/export/reports` | OrgAdminOrAbove | Export reports to Excel |

---

## Common DTO Shapes

### Paged Response
```json
{
  "items": [...],
  "total": 0
}
```

### Error Response
```json
{ "message": "Human-readable error description" }
```

### Key Request DTOs

#### CreateInvoiceRequest
```json
{
  "clientId": "guid",
  "billType": "VatBill | PanBill",
  "dueDate": "2026-01-15",
  "discountAmount": 0,
  "isAbbreviated": false,
  "buyerPan": "string",
  "buyerAddress": "string",
  "projectId": "guid?",
  "projectRemark": "string?",
  "items": [
    { "id": "guid", "lineNo": 1, "description": "string", "hsCode": "string?",
      "unit": "string", "quantity": 0, "unitPrice": 0, "projectId": "guid?" }
  ]
}
```

#### CreateMoneyReceiptRequest
```json
{
  "invoiceIds": ["guid", "guid"],
  "allocatedAmounts": [1500.00, 500.00],
  "paymentMethod": "Cash | Cheque | BankTransfer | Online | Card | QR",
  "referenceNo": "string?",
  "receivedFromName": "string",
  "receivedFromPan": "string?",
  "receivedFromAddress": "string?",
  "notes": "string"
}
```

---

## Versioning

There is **no explicit API versioning**. All endpoints live under `/api/{resource}`. Versioning should be introduced via URL prefix (`/api/v1/...`) when needed.
