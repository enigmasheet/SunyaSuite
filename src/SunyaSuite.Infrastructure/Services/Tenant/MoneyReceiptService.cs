using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class MoneyReceiptService : IMoneyReceiptService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly INepaliDateService _nepaliDateService;
    private readonly INumberToWordsService _numberToWordsService;
    private readonly IClientStatusCalculator _statusCalculator;
    private readonly IFiscalYearService _fiscalYearService;
    private readonly IBusinessProfileService _businessProfileService;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public MoneyReceiptService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        AuthenticationStateProvider authStateProvider,
        INepaliDateService nepaliDateService,
        INumberToWordsService numberToWordsService,
        IClientStatusCalculator statusCalculator,
        IFiscalYearService fiscalYearService,
        IBusinessProfileService businessProfileService,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _authStateProvider = authStateProvider;
        _nepaliDateService = nepaliDateService;
        _numberToWordsService = numberToWordsService;
        _timeProvider = timeProvider;
        _statusCalculator = statusCalculator;
        _fiscalYearService = fiscalYearService;
        _businessProfileService = businessProfileService;
        _tenantContext = tenantContext;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    public async Task<MoneyReceiptDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var receipt = await context.MoneyReceipts
            .Include(r => r.FiscalYearInfo)
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Invoice)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        return receipt is null ? null : MapToDetail(receipt);
    }

    public async Task<PagedResult<MoneyReceiptListItemDto>> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null,
        string? sortLabel = null, string? sortDirection = null,
        Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.MoneyReceipts
            .Include(r => r.FiscalYearInfo)
            .AsNoTracking()
            .Where(r => r.CompanyId == companyId && !r.IsDeleted)
            .AsQueryable();

        if (fiscalYearId.HasValue)
            query = query.Where(r => r.FiscalYearId == fiscalYearId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.ReceiptNumber.ToLower().Contains(term) ||
                r.ReceivedFromName.ToLower().Contains(term));
        }

        query = (sortLabel?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("receiptnumber", "desc") => query.OrderByDescending(r => r.ReceiptNumber),
            ("receiptnumber", _) => query.OrderBy(r => r.ReceiptNumber),
            ("date", "desc") => query.OrderByDescending(r => r.DateAD),
            ("date", _) => query.OrderBy(r => r.DateAD),
            ("amount", "desc") => query.OrderByDescending(r => r.AmountReceived),
            ("amount", _) => query.OrderBy(r => r.AmountReceived),
            _ => query.OrderByDescending(r => r.DateAD)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MoneyReceiptListItemDto>(
            items.Select(MapToListItem).ToList(), total);
    }

    public async Task<MoneyReceiptListItemDto> CreateAsync(CreateMoneyReceiptRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var userId = await GetCurrentUserIdAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var fiscalYearLabel = _nepaliDateService.GetFiscalYear(now);

        var fy = await _fiscalYearService.GetCurrentAsync(ct)
            ?? throw new InvalidOperationException("No active fiscal year configured.");

        if (!fy.IsOpen)
            throw new InvalidOperationException($"Fiscal year {fy.YearName} is closed. Cannot create receipts.");

        if (request.InvoiceIds.Length == 0)
            throw new InvalidOperationException("At least one invoice must be selected.");

        if (request.InvoiceIds.Length != request.AllocatedAmounts.Length)
            throw new InvalidOperationException("Each invoice must have a corresponding allocated amount.");

        var totalAmount = request.AllocatedAmounts.Sum();
        if (totalAmount <= 0)
            throw new InvalidOperationException("Total amount must be greater than zero.");

        var invoices = await context.Invoices
            .Include(i => i.Client)
            .ThenInclude(c => c.Invoices)
            .Where(i => request.InvoiceIds.Contains(i.Id))
            .ToListAsync(ct);

        if (invoices.Count != request.InvoiceIds.Length)
            throw new KeyNotFoundException("One or more invoices not found.");

        foreach (var invoice in invoices)
        {
            if (invoice.IsDeleted)
                throw new InvalidOperationException($"Invoice {invoice.InvoiceNumber} is deleted.");
            if (invoice.Status == InvoiceStatus.Draft)
                throw new InvalidOperationException($"Cannot record receipt against draft invoice {invoice.InvoiceNumber}.");
        }

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var receipt = new MoneyReceipt
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = _tenantContext.BranchId,
            ReceiptNumber = await GenerateNumberAsync(context, fy.YearName),
            FiscalYearId = fy.Id,
            DateAD = DateOnly.FromDateTime(now),
            DateBS = _nepaliDateService.ToNepaliDateString(now, "yyyy/MM/dd"),
            ReceivedFromName = request.ReceivedFromName,
            ReceivedFromPan = request.ReceivedFromPan,
            ReceivedFromAddress = request.ReceivedFromAddress,
            AmountReceived = totalAmount,
            AmountInWords = _numberToWordsService.ToNepaliWords(totalAmount),
            PaymentMethod = request.PaymentMethod,
            ReferenceNo = request.ReferenceNo,
            ReceivedBy = await GetCurrentUserNameAsync(),
            SellerLogoBase64 = (await _businessProfileService.GetDefaultAsync(ct))?.LogoBase64
        };

        for (int i = 0; i < request.InvoiceIds.Length; i++)
        {
            var allocation = new ReceiptInvoiceAllocation
            {
                Id = Guid.NewGuid(),
                MoneyReceiptId = receipt.Id,
                InvoiceId = request.InvoiceIds[i],
                AllocatedAmount = request.AllocatedAmounts[i]
            };
            receipt.Allocations.Add(allocation);

            var invoice = invoices.First(inv => inv.Id == request.InvoiceIds[i]);
            invoice.AmountPaid += request.AllocatedAmounts[i];

            if (invoice.AmountPaid >= invoice.Total && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;

                AuditLogHelper.Add(context, companyId, userId, "StatusChanged", "Invoice", invoice.Id.ToString(),
                    $"{invoice.InvoiceNumber}: → Paid (auto via receipt)", _timeProvider);

                invoice.Client.Status = _statusCalculator.Calculate(
                    invoice.Client.Invoices);
            }
        }

        context.MoneyReceipts.Add(receipt);

        AuditLogHelper.Add(context, companyId, userId, "MoneyReceiptCreated", "MoneyReceipt", receipt.Id.ToString(),
            $"{receipt.ReceiptNumber}: {totalAmount:C} via {request.PaymentMethod}", _timeProvider);

        await context.SaveChangesAsync(ct);

        await context.Entry(receipt).Reference(r => r.FiscalYearInfo).LoadAsync(ct);

        return MapToListItem(receipt);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var receipt = await context.MoneyReceipts
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Invoice)
                .ThenInclude(i => i.Client)
                .ThenInclude(c => c.Invoices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (receipt is null)
            throw new KeyNotFoundException($"MoneyReceipt {id} not found");

        if (receipt.IsDeleted)
            throw new InvalidOperationException("Receipt is already deleted.");

        var userId = await GetCurrentUserIdAsync();

        foreach (var allocation in receipt.Allocations)
        {
            var invoice = allocation.Invoice;
            invoice.AmountPaid -= allocation.AllocatedAmount;

            if (invoice.Status == InvoiceStatus.Paid && invoice.AmountPaid < invoice.Total)
            {
                invoice.Status = InvoiceStatus.Sent;

                invoice.Client.Status = _statusCalculator.Calculate(
                    invoice.Client.Invoices);
            }
        }

        receipt.IsDeleted = true;
        receipt.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "MoneyReceiptDeleted", "MoneyReceipt", id.ToString(),
            receipt.ReceiptNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var receipt = await context.MoneyReceipts
            .IgnoreQueryFilters()
            .Include(r => r.Allocations)
                .ThenInclude(a => a.Invoice)
                .ThenInclude(i => i.Client)
                .ThenInclude(c => c.Invoices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (receipt is null)
            throw new KeyNotFoundException($"MoneyReceipt {id} not found");

        if (!receipt.IsDeleted)
            throw new InvalidOperationException("Receipt is not deleted.");

        var userId = await GetCurrentUserIdAsync();

        foreach (var allocation in receipt.Allocations)
        {
            var invoice = allocation.Invoice;
            invoice.AmountPaid += allocation.AllocatedAmount;

            if (invoice.AmountPaid >= invoice.Total && invoice.Status != InvoiceStatus.Paid)
            {
                invoice.Status = InvoiceStatus.Paid;

                invoice.Client.Status = _statusCalculator.Calculate(
                    invoice.Client.Invoices);
            }
        }

        receipt.IsDeleted = false;
        receipt.DeletedAt = null;

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "MoneyReceiptRestored", "MoneyReceipt", id.ToString(),
            receipt.ReceiptNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<MoneyReceiptListItemDto>> GetDeletedPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.MoneyReceipts
            .IgnoreQueryFilters()
            .Include(r => r.FiscalYearInfo)
            .AsNoTracking()
            .Where(r => r.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(r =>
                r.ReceiptNumber.ToLower().Contains(term) ||
                r.ReceivedFromName.ToLower().Contains(term));
        }

        query = query.OrderByDescending(r => r.DeletedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MoneyReceiptListItemDto>(
            items.Select(MapToListItem).ToList(), total);
    }

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var receipt = await context.MoneyReceipts
            .IgnoreQueryFilters()
            .Include(r => r.Allocations)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (receipt is null)
            throw new KeyNotFoundException($"MoneyReceipt {id} not found");

        var userId = await GetCurrentUserIdAsync();
        var receiptNumber = receipt.ReceiptNumber;

        context.MoneyReceipts.Remove(receipt);

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "MoneyReceiptPermanentDeleted", "MoneyReceipt", id.ToString(),
            receiptNumber, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    private static MoneyReceiptListItemDto MapToListItem(MoneyReceipt r) => new(
        r.Id, r.ReceiptNumber, r.ReceivedFromName, r.AmountReceived, r.AmountInWords,
        r.DateAD, r.DateBS, r.PaymentMethod, r.ReferenceNo, r.IsDeleted, r.FiscalYearInfo?.YearName ?? "");

    private static MoneyReceiptDetailDto MapToDetail(MoneyReceipt r) => new(
        r.Id, r.ReceiptNumber, r.FiscalYearInfo?.YearName ?? "", r.DateAD, r.DateBS,
        r.ReceivedFromName, r.ReceivedFromPan, r.ReceivedFromAddress,
        r.AmountReceived, r.AmountInWords, r.PaymentMethod, r.ReferenceNo, r.ReceivedBy, r.SellerLogoBase64, r.IsDeleted,
        r.Allocations.Select(a => new ReceiptAllocationDto(
            a.Id, a.MoneyReceiptId, a.InvoiceId, a.MoneyReceipt.ReceiptNumber, a.MoneyReceipt.FiscalYearInfo?.YearName ?? "", a.AllocatedAmount, a.Invoice.InvoiceNumber)).ToList());

    private async Task<string> GenerateNumberAsync(ApplicationDbContext context, string fiscalYear)
    {
        var safeFiscalYear = fiscalYear.Replace("/", "_");
        if (safeFiscalYear.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            safeFiscalYear = "Unknown";

        var sequenceName = $"MR_Sequence_{safeFiscalYear}";

#pragma warning disable EF1002
        await context.Database.ExecuteSqlRawAsync(
            $"CREATE SEQUENCE IF NOT EXISTS \"{sequenceName}\" START 1");

        var nextSeq = await context.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{sequenceName}\"')")
            .FirstAsync();
#pragma warning restore EF1002

        return $"MR-{fiscalYear}-{nextSeq:D4}";
    }

    private Task<string> GetCurrentUserIdAsync()
        => TenantServiceHelper.GetCurrentUserIdAsync(_authStateProvider);

    private Task<string> GetCurrentUserNameAsync()
        => TenantServiceHelper.GetCurrentUserNameAsync(_authStateProvider);
}
