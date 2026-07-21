# SunyaSuite Testing

## Current State

**No test projects exist in this solution.** There are no unit, integration, or end-to-end tests anywhere in the repository.

---

## Available Test Packages

The following test packages are already declared in `Directory.Packages.props` (central package management) but are not yet referenced by any project:

| Package | Version | Purpose |
|---------|---------|---------|
| `xunit` | 2.9.3 | Unit testing framework |
| `Microsoft.NET.Test.Sdk` | 17.13.0 | Test runner SDK |
| `bunit` | 1.38.1 | Blazor component testing (for Web.Client) |
| `Moq` | 4.20.72 | Mocking framework |
| `FluentAssertions` | 7.2.0 | Assertion library |

---

## Recommended Test Structure

A `tests/` folder should be created at solution root with these projects:

```
tests/
├── SunyaSuite.Domain.Tests/          (xunit)
├── SunyaSuite.Application.Tests/     (xunit + Moq + FluentAssertions)
├── SunyaSuite.Infrastructure.Tests/  (xunit + Moq + FluentAssertions)
├── SunyaSuite.Web.Api.Tests/         (xunit + Moq + FluentAssertions)
└── SunyaSuite.Web.Client.Tests/      (bunit + xunit)
```

### What to test (by priority)

**Priority 1 — Domain logic (entities with behavior):**
- `Invoice.Recalculate()` — VAT bill vs PAN bill, abbreviated, discount, zero items
- `Invoice.RecordPayment()` / `ReversePayment()` — positive validation, bounds
- `Invoice.IsFullyPaid` — after payment recording
- `Project.ProgressPercent` — clamping
- `InvoiceItem.Amount` — computation

**Priority 2 — Service logic (application rules):**
- `ClientStatusCalculator.Calculate()` — red/yellow/green scenarios
- `InvoiceService` status transitions (Draft→Sent, etc.)
- `FiscalYearService` — only one current, toggle open

**Priority 3 — Validators:**
- `CreateClientRequestValidator` — empty name, invalid email
- `CreateInvoiceRequestValidator` — empty items, date in past, missing client
- All 6 validators in `Application/Validators/`

**Priority 4 — Integration:**
- DbContext factory + query filtering (soft-delete, company scoping)
- `SeedData.InitializeAsync()` — idempotency

**Priority 5 — Blazor components (bUnit):**
- Shared components: `ErrorAlert`, `LoadingSkeleton`, `FormActions`, `StatusChip`
- Page-level smoke tests: renders without exception

---

## Mocking Conventions

- Use **Moq** for all interface mocking
- Use `Mock.Of<T>()` for simple stubs
- Use `new Mock<T>()` + `Setup()`/`Verify()` for behavioral tests
- Do **not** mock `DbContext` directly — use EF Core InMemory provider for integration-level tests

---

## How to Run Tests

```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run specific project
dotnet test tests/SunyaSuite.Domain.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Coverage Expectations

| Area | Minimum Target |
|------|---------------|
| Domain entities (with logic) | 90% |
| Validators | 85% |
| Service layer (critical paths) | 70% |
| Blazor components | 50% (smoke tests) |
| Infrastructure (PDF, email) | 30% (integration) |

---

## Coverage Gaps

1. No tests exist at all — full gap
2. Invoice state machine (status transitions) is untested
3. Client status calculator has no test coverage
4. Soft-delete cascade behavior is untested
5. Tenant DbContextFactory multi-tenancy scoping has no coverage
6. No Blazor component tests (bUnit available, unused)
7. No E2E tests (Playwright or similar)

---

## CI Integration

The `.github/workflows/` directory exists but is empty. A basic CI workflow should be added to:
1. Restore → Build → Test on push/PR
2. Run tests with coverage
3. Fail on coverage below thresholds

---

## Note on Manual Testing

Currently, the application is tested manually via:
- `run.ps1` starts both API and WASM client
- Swagger available at `http://localhost:5000/openapi/v1.json`
- Client at `http://localhost:5002`
- Default admin: `admin@sunya.local` / `Admin@123`
