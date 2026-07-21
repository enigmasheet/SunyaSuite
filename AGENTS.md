## Quick Reference

For comprehensive documentation about this codebase, see the `docs/` folder:

- [CLAUDE.md](docs/CLAUDE.md) — Entry point for AI agents: links to all docs, build commands, dos/don'ts
- [ARCHITECTURE.md](docs/ARCHITECTURE.md) — Solution structure, project references, layering, multi-tenancy
- [CONVENTIONS.md](docs/CONVENTIONS.md) — Naming, DI, error handling, async, nullable patterns
- [DOMAIN.md](docs/DOMAIN.md) — Business entities, glossary, business rules
- [DATA-MODEL.md](docs/DATA-MODEL.md) — DB schema, EF Core mappings, migration workflow
- [API-CONTRACTS.md](docs/API-CONTRACTS.md) — Endpoints, DTOs, auth, versioning
- [TESTING.md](docs/TESTING.md) — Test project structure, how to run tests, coverage
- [DEPENDENCIES.md](docs/DEPENDENCIES.md) — NuGet packages and their purpose

## Skills

This project uses a project-specific skill that documents architectural conventions, Blazor Server best practices, and .NET 10-specific features. Load it before any code changes:

- **`blazor-sunyasuite-conventions`** — Program.cs wiring order, Clean Architecture layering, Identity setup, MudBlazor patterns, EF Core via `IDbContextFactory`, background services, Blazor Server rendering/state/JS interop best practices, and .NET 10 target-awareness.
- **`architect-review`** — Master software architect for architecture reviews, scalability/resilience assessment, pattern compliance, Clean Architecture/DDD/microservices evaluation, and architectural guidance. Use for significant design changes or system-level decisions.

## Form Page Template

Use this template when creating new Create/Edit pages to keep form patterns consistent across the codebase.

### Template: Create page (simple — no async data load)

Use for forms whose fields don't depend on loaded data (no dropdowns from DB).

```razor
@page "/{entities}/create"
@attribute [Authorize(Policy = PolicyNames.X)]
@inject I{Entity}Service {Entity}Service
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<PageHeader Title="Create {Entity}" />

<ErrorAlert Message="@_errorMessage" />

<FormCard>
    <MudForm @ref="_form" @bind-IsValid="_isValid">
        <MudGrid>
            <MudItem xs="12" sm="6">
                <MudTextField @bind-Value="_field"
                              Label="Field Name"
                              Required="true"
                              RequiredError="{Field} is required"
                              For="@(() => _field)" />
            </MudItem>
        </MudGrid>

        <FormActions SaveLabel="Create {Entity}"
                     CancelHref="/{entities}"
                     Disabled="@(!_isValid)"
                     OnSave="@(async () => await SaveAsync())" />
    </MudForm>
</FormCard>

@code {
    private MudForm _form = null!;
    private bool _isValid;
    private string? _errorMessage;

    // Form fields stored as simple properties/fields
    // (not a DTO — MudForm validation binds to individual fields)

    private async Task SaveAsync()
    {
        await _form.ValidateAsync();
        if (!_isValid) return;

        try
        {
            _errorMessage = null;
            await {Entity}Service.CreateAsync(request);
            Snackbar.Add("{Entity} created successfully.", Severity.Success);
            Navigation.NavigateTo("/{entities}");
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to create {entity}: {ex.Message}";
        }
    }
}
```

**Key rules:**
- No `_loading` state, no `LoadingSkeleton` — this page loads nothing
- No `_saving` state unless the save is slow enough to warrant a spinner
- Errors that come from the server during `CreateAsync` or dialog actions go to `Snackbar` (not `_errorMessage`) if they're non-blocking; `_errorMessage` is for loading/blocking errors shown at the top
- Cancel goes to the list page (`/entities`), not detail page

### Template: Create page (with async data load)

Use for forms that need to load dropdown options (e.g. client list for invoice create).

```razor
@page "/{entities}/create"
@attribute [Authorize(Policy = PolicyNames.X)]
@inject I{Entity}Service {Entity}Service
@inject IDropdownService DropdownService
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

<PageHeader Title="Create {Entity}" />

<ErrorAlert Message="@_errorMessage" />

@if (_loading)
{
    <LoadingSkeleton Variant="SkeletonVariant.Form" />
}
else
{
    <FormCard>
        <MudForm @ref="_form" @bind-IsValid="_isValid">
            <MudGrid>
                <MudItem xs="12" sm="6">
                    <MudSelect @bind-Value="_selectedItem"
                               Label="Parent Item" Required="true"
                               RequiredError="Selection is required"
                               For="@(() => _selectedItem)">
                        @foreach (var item in _items)
                        {
                            <MudSelectItem Value="@item.Id" @key="item.Id">@item.Name</MudSelectItem>
                        }
                    </MudSelect>
                </MudItem>
            </MudGrid>

            <FormActions SaveLabel="Create {Entity}"
                         CancelHref="/{entities}"
                         Disabled="@(!_isValid)"
                         OnSave="@(async () => await SaveAsync())" />
        </MudForm>
    </FormCard>
}

@code {
    private MudForm _form = null!;
    private bool _isValid;
    private bool _loading = true;
    private string? _errorMessage;

    private List<DropdownDto> _items = [];
    private Guid _selectedItem;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _items = await DropdownService.GetOptionsAsync();
        }
        catch
        {
            _errorMessage = "Failed to load data.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveAsync()
    {
        await _form.ValidateAsync();
        if (!_isValid) return;

        try
        {
            await {Entity}Service.CreateAsync(/* request */);
            Snackbar.Add("{Entity} created successfully.", Severity.Success);
            Navigation.NavigateTo("/{entities}");
        }
        catch (Exception ex)
        {
            // Prefer Snackbar for server errors over _errorMessage
            Snackbar.Add($"Failed to create {entity}: {ex.Message}", Severity.Error);
        }
    }
}
```

**Key rules:**
- `_errorMessage` for load failures (shown above skeleton)
- `Snackbar` for save failures (shown as toast, not inline)
- Use `@key="item.Id"` on `MudSelectItem` inside `@foreach` for proper diffing

### Template: Edit page

```razor
@page "/{entities}/edit/{Id:guid}"
@attribute [Authorize(Policy = PolicyNames.X)]
@inject I{Entity}Service {Entity}Service
@inject ISnackbar Snackbar
@inject NavigationManager Navigation

@* Dynamic title: shows entity name once loaded *@
<PageHeader Title="@(_model is null ? "Edit {Entity}" : $"Edit: {_model.Name}")" />

<ErrorAlert Message="@_errorMessage"
            OnDismiss="@(() => _errorMessage = null)" />

@if (_loading)
{
    <LoadingSkeleton Variant="SkeletonVariant.Form" />
}
else if (_model is null)
{
    <MudAlert Severity="Severity.Warning" Class="mt-4">{Entity} not found.</MudAlert>
}
else
{
    <FormCard>
        <MudForm @ref="_form" @bind-IsValid="_isValid">
            <MudGrid>
                @* Fields bound to component fields, populated from _model in OnInitializedAsync *@
                <MudItem xs="12" sm="6">
                    <MudTextField @bind-Value="_field"
                                  Label="Field Name"
                                  Required="true"
                                  RequiredError="{Field} is required"
                                  For="@(() => _field)" />
                </MudItem>
            </MudGrid>

            <FormActions SaveLabel="Save Changes"
                         CancelHref="@($"/{entities}/{Id}")"
                         Disabled="@(!_isValid)"
                         OnSave="@(async () => await SaveAsync())" />
        </MudForm>
    </FormCard>
}

@code {
    [Parameter] public Guid Id { get; set; }  @* Use string for non-Guid PKs *@

    private MudForm _form = null!;
    private bool _isValid;
    private bool _loading = true;
    private string? _errorMessage;
    private {Entity}Dto? _model;

    // Form fields (same names as Create page)

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _model = await {Entity}Service.GetByIdAsync(Id);
            if (_model is not null)
            {
                // Populate form fields from _model
            }
        }
        catch
        {
            _errorMessage = "Failed to load data.";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task SaveAsync()
    {
        await _form.ValidateAsync();
        if (!_isValid) return;

        try
        {
            await {Entity}Service.UpdateAsync(request);
            Snackbar.Add("{Entity} updated.", Severity.Success);
            Navigation.NavigateTo($"/{entities}/{Id}");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to update: {ex.Message}", Severity.Error);
        }
    }
}
```

**Key rules:**
- Always has `_loading` + `LoadingSkeleton` (entity must be fetched first)
- Always guard `_model is null` with a not-found MudAlert
- CancelHref points to detail page (`/{entity}/{Id}`), not the list page
- Dynamic title with fallback: `@(_model is null ? "Edit {Entity}" : $"Edit: {_model.Name}")`
- `OnDismiss="@(() => _errorMessage = null)"` on ErrorAlert lets the user clear the error

### Complex form patterns

#### Dynamic lists (line items)

Used by Invoices (and potentially other multi-line forms). Pattern:

```razor
@for (int i = 0; i < _items.Count; i++)
{
    var idx = i;
    <MudGrid Class="mb-2" @key="@_items[idx].Id">
        <MudItem xs="12" sm="4">
            <MudTextField @bind-Value="_items[idx].Description" Label="Description" />
        </MudItem>
        <MudItem xs="12" sm="2">
            <MudNumericField @bind-Value="_items[idx].Quantity" Label="Qty" Min="1" />
        </MudItem>
        <MudItem xs="12" sm="2">
            <MudNumericField @bind-Value="_items[idx].UnitPrice" Label="Unit Price" Min="0" />
        </MudItem>
        <MudItem xs="12" sm="1" Class="d-flex align-center">
            <MudIconButton Icon="@Icons.Material.Filled.Delete" Color="Color.Error"
                           OnClick="@(() => RemoveItem(idx))" />
        </MudItem>
    </MudGrid>
}
<MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="@AddItem">Add Item</MudButton>
```

**Key rules:**
- Use `var idx = i;` to capture the loop variable (don't use `i` directly in lambdas)
- Use `@key="@_items[idx].Id"` with a stable identifier (Guid), not the index
- Each item model should have an `Id` (initialized to `Guid.NewGuid()` on creation)
- Remove: `_items.RemoveAt(index)` — no need to reassign collection, Blazor detects via `@key`

#### Conditional sections

```razor
@if (_condition)
{
    <MudGrid>
        <MudItem xs="12" sm="6">
            <MudTextField @bind-Value="_conditionalField" Label="Conditional Field" />
        </MudItem>
    </MudGrid>
}
```

**Key rules:**
- Keep condition logic simple — a single `bool` property or enum comparison
- For enums, use pattern `@if (_billType == BillType.VatBill)` (not string comparisons)

#### Read-only fields in Edit

```razor
<MudItem xs="12" sm="6">
    <MudText Typo="Typo.body2" Class="text-secondary">Label</MudText>
    <MudText Typo="Typo.body1">@_model.Value</MudText>
</MudItem>
```

Show the value as `MudText` instead of a `MudTextField` when the field should not be editable.

### Standard field grid layout

| Column count | MudItem size | Use case |
|---|---|---|
| 1 column | `xs="12"` | Full-width fields (address, description, notes) |
| 2 columns | `xs="12" sm="6"` | Standard labels (name, email) — stacks on mobile |
| 3 columns | `xs="12" md="4"` | Dense layouts |
| 4 columns | `xs="12" sm="6" md="3"` | Small fields (PAN, reference no) |

### Save handler variations

| Pattern | When to use | Example pages |
|---|---|---|
| `_errorMessage` for failures | Simple pages, blocking errors | Clients/Create, Clients/Edit |
| `Snackbar` for failures | Complex pages, non-blocking errors | Invoices/Create, Users/Create |
| `_saving` + `Saving` param | Slow saves (spinner in FormActions) | Users/Create, Users/Edit |
| `_saving` in try/finally | Always pair with `_saving` state | Users/Create, Users/Edit |

### Common mistakes to avoid

- ❌ Injecting `NavigationManager` and not using it in markup or code-behind — remove the injection
- ❌ Passing `Visible="@(_errorMessage is not null)"` on `ErrorAlert` — that parameter doesn't exist; `ErrorAlert` already checks internally via `!string.IsNullOrEmpty(Message)`
- ❌ `Message="_errorMessage"` without `@` prefix — this passes the literal string `"_errorMessage"`, not the C# variable; always use `Message="@_errorMessage"`
- ❌ Adding `LoadingSkeleton` to a Create page that doesn't load async data — no skeleton needed
- ❌ Using `_errorMessage` for save errors AND load errors simultaneously — use `_errorMessage` for load errors, `Snackbar` for save errors to avoid stale error after successful save
- ❌ Removing `_errorMessage = null` reset before save — if you use `_errorMessage` for save errors, reset it in the try block before calling the service, not after, to prevent stale error flash
