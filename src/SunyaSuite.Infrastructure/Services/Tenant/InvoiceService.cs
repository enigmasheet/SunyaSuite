using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class InvoiceService : IInvoiceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IClientStatusCalculator _statusCalculator;
    private readonly INepaliDateService _nepaliDateService;
    private readonly INumberToWordsService _numberToWordsService;
    private readonly IFiscalYearService _fiscalYearService;
    private readonly VatSettings _vatSettings;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public InvoiceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        AuthenticationStateProvider authStateProvider,
        IClientStatusCalculator statusCalculator,
        INepaliDateService nepaliDateService,
        INumberToWordsService numberToWordsService,
        IFiscalYearService fiscalYearService,
        IOptions<VatSettings> vatSettings,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _authStateProvider = authStateProvider;
        _statusCalculator = statusCalculator;
        _timeProvider = timeProvider;
        _nepaliDateService = nepaliDateService;
        _numberToWordsService = numberToWordsService;
        _fiscalYearService = fiscalYearService;
        _vatSettings = vatSettings.Value;
        _tenantContext = tenantContext;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    private Task<decimal> GetVatRateAsync(CancellationToken ct = default)
    {
        var rate = _vatSettings.Rate;
        return Task.FromResult(rate > 0 ? rate : 13m);
    }

    private Task<string> GetCurrentUserIdAsync()
        => TenantServiceHelper.GetCurrentUserIdAsync(_authStateProvider);

    public async Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
                .ThenInclude(it => it.Project)
            .Include(i => i.CompanyInfo)
            .Include(i => i.FiscalYearInfo)
            .Include(i => i.Project)
            .Include(i => i.ReceiptAllocations)
                .ThenInclude(a => a.MoneyReceipt)
                .ThenInclude(mr => mr.FiscalYearInfo)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        return invoice is null ? null : MapToDetail(invoice);
    }

    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(
        int page, int pageSize, string? sortLabel, string? sortDirection,
        string? searchTerm = null, InvoiceFilterDto? filter = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.Invoices
            .Include(i => i.Client)
            .Include(i => i.FiscalYearInfo)
            .Include(i => i.Project)
            .AsNoTracking()
            .Where(i => i.CompanyId == companyId && !i.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term) ||
                i.Client.Name.ToLower().Contains(term));
        }

        if (filter?.Statuses is { Count: > 0 })
            query = query.Where(i => filter.Statuses.Contains(i.Status));

        if (filter?.IssueDateFrom is not null)
            query = query.Where(i => i.IssueDate >= filter.IssueDateFrom.Value);

        if (filter?.IssueDateTo is not null)
            query = query.Where(i => i.IssueDate <= filter.IssueDateTo.Value);

        if (filter?.DueDateFrom is not null)
            query = query.Where(i => i.DueDate >= filter.DueDateFrom.Value);

        if (filter?.DueDateTo is not null)
            query = query.Where(i => i.DueDate <= filter.DueDateTo.Value);

        if (filter?.ClientId is not null)
            query = query.Where(i => i.ClientId == filter.ClientId.Value);

        if (filter?.FiscalYearId is not null)
            query = query.Where(i => i.FiscalYearId == filter.FiscalYearId.Value);

        query = (sortLabel?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("invoicenumber", "desc") => query.OrderByDescending(i => i.InvoiceNumber),
            ("invoicenumber", _) => query.OrderBy(i => i.InvoiceNumber),
            ("client", "desc") => query.OrderByDescending(i => i.Client.Name),
            ("client", _) => query.OrderBy(i => i.Client.Name),
            ("status", "desc") => query.OrderByDescending(i => i.Status),
            ("status", _) => query.OrderBy(i => i.Status),
            ("duedate", "desc") => query.OrderByDescending(i => i.DueDate),
            ("duedate", _) => query.OrderBy(i => i.DueDate),
            ("total", "desc") => query.OrderByDescending(i => i.Total),
            ("total", _) => query.OrderBy(i => i.Total),
            _ => query.OrderByDescending(i => i.IssueDate)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<InvoiceListItemDto>(
            items.Select(MapToListItem).ToList(), total);
    }

    public async Task<InvoiceListItemDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var userId = await GetCurrentUserIdAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var fiscalYearLabel = _nepaliDateService.GetFiscalYear(now);

        var fy = await _fiscalYearService.GetCurrentAsync(ct)
            ?? throw new InvalidOperationException("No active fiscal year configured.");

        if (!fy.IsOpen)
            throw new InvalidOperationException($"Fiscal year {fy.YearName} is closed. Cannot create invoices.");

        var vatRate = await GetVatRateAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var company = await context.Companies.AsNoTracking()
            .FirstAsync(c => c.Id == companyId, ct);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = _tenantContext.BranchId,
            ClientId = request.ClientId,
            BillType = request.BillType,
            InvoiceNumber = await GenerateNumberAsync(context, fy.YearName),
            FiscalYearId = fy.Id,
            DateBS = _nepaliDateService.ToNepaliDateString(now, "yyyy/MM/dd"),
            IssueDate = DateOnly.FromDateTime(now),
            DueDate = request.DueDate,
            TaxRate = vatRate,
            DiscountAmount = request.DiscountAmount,
            IsAbbreviated = request.IsAbbreviated,
            Status = InvoiceStatus.Draft,
            ProjectId = request.ProjectId,
            ProjectRemark = request.ProjectRemark,
            BuyerPan = request.BuyerPan,
            BuyerAddress = request.BuyerAddress,
            SellerName = company.Name,
            SellerPan = company.PanNumber,
            SellerAddress = company.Address,
            SellerPhone = company.Phone,
            SellerLogoBase64 = company.LogoBase64
        };

        invoice.Items = request.Items.Select((item, idx) => new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            LineNo = item.LineNo > 0 ? item.LineNo : idx + 1,
            Description = item.Description,
            HsCode = item.HsCode,
            Unit = item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            ProjectId = item.ProjectId
        }).ToList();

        invoice.Recalculate(vatRate);
        invoice.GrandTotalInWords = _numberToWordsService.ToNepaliWords(invoice.Total);

        context.Invoices.Add(invoice);
        AuditLogHelper.Add(context, companyId, userId, "Created", "Invoice", invoice.Id.ToString(), $"{invoice.InvoiceNumber} - {invoice.Total:C}", _timeProvider);

        await context.SaveChangesAsync(ct);

        await context.Entry(invoice).Reference(i => i.FiscalYearInfo).LoadAsync(ct);

        return MapToListItem(invoice);
    }

    public async Task<InvoiceListItemDto> UpdateAsync(UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct);

        if (existing is null)
            throw new KeyNotFoundException($"Invoice {request.Id} not found");

        if (existing.IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted invoice.");

        if (existing.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be edited.");

        var vatRate = await GetVatRateAsync(ct);

        context.InvoiceItems.RemoveRange(existing.Items);

        existing.DueDate = request.DueDate;
        existing.TaxRate = vatRate;
        existing.DiscountAmount = request.DiscountAmount;
        existing.IsAbbreviated = request.IsAbbreviated;
        existing.ProjectId = request.ProjectId;
        existing.ProjectRemark = request.ProjectRemark;
        existing.BuyerPan = request.BuyerPan;
        existing.BuyerAddress = request.BuyerAddress;

        existing.Items = request.Items.Select((item, idx) => new InvoiceItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = existing.Id,
            LineNo = item.LineNo > 0 ? item.LineNo : idx + 1,
            Description = item.Description,
            HsCode = item.HsCode,
            Unit = item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            ProjectId = item.ProjectId
        }).ToList();

        existing.Recalculate(vatRate);
        existing.GrandTotalInWords = _numberToWordsService.ToNepaliWords(existing.Total);

        var userId = await GetCurrentUserIdAsync();
        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "Updated", "Invoice", existing.Id.ToString(), existing.InvoiceNumber, _timeProvider);

        await context.SaveChangesAsync(ct);

        return MapToListItem(existing);
    }

    private static readonly Dictionary<InvoiceStatus, HashSet<InvoiceStatus>> AllowedStatusTransitions = new()
    {
        [InvoiceStatus.Draft] = [InvoiceStatus.Sent],
        [InvoiceStatus.Sent] = [InvoiceStatus.Overdue, InvoiceStatus.Paid],
        [InvoiceStatus.Overdue] = [InvoiceStatus.Paid],
        [InvoiceStatus.Paid] = []
    };

    public async Task<List<InvoiceSelectionDto>> GetInvoiceSelectionAsync(Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Invoices
            .Include(i => i.Client)
            .AsNoTracking()
            .Where(i => !i.IsDeleted && i.Status != InvoiceStatus.Draft)
            .AsQueryable();

        if (fiscalYearId.HasValue)
            query = query.Where(i => i.FiscalYearId == fiscalYearId.Value);

        return await query
            .OrderByDescending(i => i.IssueDate)
            .Select(i => new InvoiceSelectionDto(
                i.Id, i.InvoiceNumber, i.Client.Name, i.Total, i.AmountPaid, i.FiscalYearInfo.YearName))
            .ToListAsync(ct);
    }

    public async Task UpdateStatusAsync(Guid id, InvoiceStatus status, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Invoices
            .Include(i => i.Client)
            .ThenInclude(c => c.Invoices)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        if (existing is null)
            throw new KeyNotFoundException($"Invoice {id} not found");

        if (existing.IsDeleted)
            throw new InvalidOperationException("Cannot update status of a deleted invoice.");

        var previousStatus = existing.Status;
        if (previousStatus == status)
            return;

        if (!AllowedStatusTransitions.TryGetValue(previousStatus, out var allowed) || !allowed.Contains(status))
            throw new InvalidOperationException($"Cannot transition invoice from {previousStatus} to {status}.");

        existing.Status = status;

        existing.Client.Status = _statusCalculator.Calculate(
            existing.Client.Invoices);

        var userId = await GetCurrentUserIdAsync();
        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "StatusChanged", "Invoice", existing.Id.ToString(),
            $"{existing.InvoiceNumber}: {previousStatus} → {status}", _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.IsDeleted)
            throw new InvalidOperationException("Invoice is already deleted.");

        var userId = await GetCurrentUserIdAsync();
        var invoiceNumber = invoice.InvoiceNumber;

        invoice.IsDeleted = true;
        invoice.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "SoftDeleted", "Invoice", id.ToString(), invoiceNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<DeletedInvoiceDto>> GetDeletedPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Invoices
            .IgnoreQueryFilters()
            .Include(i => i.Client)
            .Include(i => i.FiscalYearInfo)
            .AsNoTracking()
            .Where(i => i.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(i =>
                i.InvoiceNumber.ToLower().Contains(term) ||
                i.Client.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(i => i.DeletedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DeletedInvoiceDto>(
            items.Select(MapToDeleted).ToList(), total);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            throw new KeyNotFoundException($"Invoice {id} not found");

        if (!invoice.IsDeleted)
            throw new InvalidOperationException("Invoice is not deleted.");

        var userId = await GetCurrentUserIdAsync();

        invoice.IsDeleted = false;
        invoice.DeletedAt = null;

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "Restored", "Invoice", id.ToString(), invoice.InvoiceNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices.IgnoreQueryFilters().FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null)
            throw new KeyNotFoundException($"Invoice {id} not found");

        var userId = await GetCurrentUserIdAsync();
        var invoiceNumber = invoice.InvoiceNumber;

        context.Invoices.Remove(invoice);
        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "PermanentDeleted", "Invoice", id.ToString(), invoiceNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    private static InvoiceListItemDto MapToListItem(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.FiscalYearInfo?.YearName ?? "", i.BillType, i.ClientId, i.Client?.Name ?? "",
        i.IssueDate, i.DueDate, i.Total, i.Status.ToString(), i.Project?.Name);

    private static InvoiceDetailDto MapToDetail(Invoice i) => new(
        i.Id, i.ClientId, i.Client?.Name ?? "", i.Client?.Email ?? "", i.InvoiceNumber, i.FiscalYearInfo?.YearName ?? "", i.BillType,
        i.IssueDate, i.DueDate, i.DateBS, i.Subtotal, i.TaxRate, i.DiscountAmount,
        i.VatAmount, i.Total, i.GrandTotalInWords, i.IsAbbreviated, i.Status.ToString(),
        i.BuyerPan, i.BuyerAddress, i.SellerName, i.SellerPan, i.SellerAddress, i.SellerPhone, i.SellerLogoBase64,
        i.CompanyInfo?.Name ?? "", i.CompanyInfo?.Address ?? "", i.CompanyInfo?.PanNumber ?? "", i.CompanyInfo?.Phone ?? "",
        i.ProjectId, i.Project?.Name, i.ProjectRemark,
        i.Items.OrderBy(it => it.LineNo).Select(item => new InvoiceItemDto
        {
            Id = item.Id,
            LineNo = item.LineNo,
            Description = item.Description,
            HsCode = item.HsCode,
            Unit = item.Unit,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            ProjectId = item.ProjectId,
            ProjectName = item.Project?.Name
        }).ToList(),
        i.AmountPaid,
        i.ReceiptAllocations.OrderByDescending(a => a.MoneyReceipt.DateAD).Select(a => new ReceiptAllocationDto(
            a.Id, a.MoneyReceiptId, a.InvoiceId, a.MoneyReceipt.ReceiptNumber, a.MoneyReceipt.FiscalYearInfo?.YearName ?? "", a.AllocatedAmount, i.InvoiceNumber)).ToList());

    private static DeletedInvoiceDto MapToDeleted(Invoice i) => new(
        i.Id, i.InvoiceNumber, i.Total, i.FiscalYearInfo?.YearName ?? "", i.BillType, i.DeletedAt);

    private static async Task<string> GenerateNumberAsync(ApplicationDbContext context, string fiscalYear)
    {
        var safeFiscalYear = fiscalYear.Replace("/", "_");
        if (safeFiscalYear.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            safeFiscalYear = "Unknown";

        var sequenceName = $"InvoiceSequence_{safeFiscalYear}";

#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync(
            $"CREATE SEQUENCE IF NOT EXISTS \"{sequenceName}\" START 1");

        var nextSeq = await context.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{sequenceName}\"') AS \"Value\"")
            .FirstAsync();
#pragma warning restore EF1002

        return $"{fiscalYear}-{nextSeq:D5}";
    }
}
