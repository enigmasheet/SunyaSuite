using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class ExportService : IExportService
{
    private const int MaxExportRows = 10000;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ILogger<ExportService> _logger;
    private readonly TimeProvider _timeProvider;

    public ExportService(IDbContextFactory<ApplicationDbContext> contextFactory, ILogger<ExportService> logger, TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<byte[]> ExportClientsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Clients
            .Where(c => !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(ct);
        if (total > MaxExportRows)
            _logger.LogWarning("Exporting {Count} clients (max {Max}). Consider filtering.", total, MaxExportRows);

        var clients = await query.Take(MaxExportRows).ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Clients");

        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Company";
        ws.Cell(1, 3).Value = "Email";
        ws.Cell(1, 4).Value = "Phone";
        ws.Cell(1, 5).Value = "Address";
        ws.Cell(1, 6).Value = "Status";
        ws.Cell(1, 7).Value = "Created At";

        StyleHeader(ws.Range(1, 1, 1, 7));

        int row = 2;
        foreach (var c in clients)
        {
            ws.Cell(row, 1).Value = c.Name;
            ws.Cell(row, 2).Value = c.Company;
            ws.Cell(row, 3).Value = c.Email;
            ws.Cell(row, 4).Value = c.Phone;
            ws.Cell(row, 5).Value = c.Address;
            ws.Cell(row, 6).Value = c.Status.ToString();
            ws.Cell(row, 7).Value = c.CreatedAt.ToString("yyyy-MM-dd");
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveToBytes(workbook);
    }

    public async Task<byte[]> ExportProjectsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var projects = await context.Projects
            .Include(p => p.Client)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.Deadline)
            .Take(MaxExportRows)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Projects");

        ws.Cell(1, 1).Value = "Name";
        ws.Cell(1, 2).Value = "Client";
        ws.Cell(1, 3).Value = "Description";
        ws.Cell(1, 4).Value = "Deadline";
        ws.Cell(1, 5).Value = "Progress";
        ws.Cell(1, 6).Value = "Status";

        StyleHeader(ws.Range(1, 1, 1, 6));

        int row = 2;
        foreach (var p in projects)
        {
            ws.Cell(row, 1).Value = p.Name;
            ws.Cell(row, 2).Value = p.Client?.Name ?? "";
            ws.Cell(row, 3).Value = p.Description;
            ws.Cell(row, 4).Value = p.Deadline.ToString("yyyy-MM-dd");
            ws.Cell(row, 5).Value = p.ProgressPercent;
            ws.Cell(row, 6).Value = p.Status.ToString();
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveToBytes(workbook);
    }

    public async Task<byte[]> ExportInvoicesAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var invoices = await context.Invoices
            .Include(i => i.Client)
            .Include(i => i.Items)
            .Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IssueDate)
            .Take(MaxExportRows)
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Invoices");

        ws.Cell(1, 1).Value = "Invoice #";
        ws.Cell(1, 2).Value = "Client";
        ws.Cell(1, 3).Value = "Issue Date";
        ws.Cell(1, 4).Value = "Due Date";
        ws.Cell(1, 5).Value = "Subtotal";
        ws.Cell(1, 6).Value = "Tax Rate";
        ws.Cell(1, 7).Value = "Discount";
        ws.Cell(1, 8).Value = "Total";
        ws.Cell(1, 9).Value = "Status";
        ws.Cell(1, 10).Value = "Item Count";

        StyleHeader(ws.Range(1, 1, 1, 10));

        int row = 2;
        foreach (var inv in invoices)
        {
            ws.Cell(row, 1).Value = inv.InvoiceNumber;
            ws.Cell(row, 2).Value = inv.Client?.Name ?? "";
            ws.Cell(row, 3).Value = inv.IssueDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 4).Value = inv.DueDate.ToString("yyyy-MM-dd");
            ws.Cell(row, 5).Value = inv.Subtotal;
            ws.Cell(row, 6).Value = inv.TaxRate;
            ws.Cell(row, 7).Value = inv.DiscountAmount;
            ws.Cell(row, 8).Value = inv.Total;
            ws.Cell(row, 9).Value = inv.Status.ToString();
            ws.Cell(row, 10).Value = inv.Items.Count;
            row++;
        }

        ws.Columns().AdjustToContents();
        return SaveToBytes(workbook);
    }

    public async Task<byte[]> ExportReportsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var totalClients = await context.Clients.CountAsync(c => !c.IsDeleted, ct);
        var activeProjects = await context.Projects.CountAsync(p => !p.IsDeleted && p.Status != ProjectStatus.Completed, ct);

        var paidInvoices = await context.Invoices.CountAsync(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid, ct);
        var overdueInvoices = await context.Invoices.CountAsync(i => !i.IsDeleted && i.Status == InvoiceStatus.Overdue, ct);
        var outstandingInvoices = await context.Invoices.CountAsync(
            i => !i.IsDeleted && (i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue), ct);
        var revenueMonth = await context.Invoices
            .Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid && i.IssueDate >= monthStart)
            .SumAsync(i => (decimal?)i.Total, ct) ?? 0m;
        var revenueTotal = await context.Invoices
            .Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid)
            .SumAsync(i => (decimal?)i.Total, ct) ?? 0m;

        var clientBreakdown = await context.Clients
            .Where(c => !c.IsDeleted)
            .GroupBy(c => c.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        var projectBreakdown = await context.Projects
            .Where(p => !p.IsDeleted)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
            .ToListAsync(ct);

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell(1, 1).Value = "Metric";
        summary.Cell(1, 2).Value = "Value";
        StyleHeader(summary.Range(1, 1, 1, 2));

        summary.Cell(2, 1).Value = "Total Clients";
        summary.Cell(2, 2).Value = totalClients;
        summary.Cell(3, 1).Value = "Active Projects";
        summary.Cell(3, 2).Value = activeProjects;
        summary.Cell(4, 1).Value = "Paid Invoices";
        summary.Cell(4, 2).Value = paidInvoices;
        summary.Cell(5, 1).Value = "Overdue Invoices";
        summary.Cell(5, 2).Value = overdueInvoices;
        summary.Cell(6, 1).Value = "Outstanding Invoices";
        summary.Cell(6, 2).Value = outstandingInvoices;
        summary.Cell(7, 1).Value = "Revenue This Month";
        summary.Cell(7, 2).Value = revenueMonth;
        summary.Cell(8, 1).Value = "Total Revenue";
        summary.Cell(8, 2).Value = revenueTotal;
        summary.Columns().AdjustToContents();

        // Client breakdown sheet
        var clientWs = workbook.Worksheets.Add("Client Status");
        clientWs.Cell(1, 1).Value = "Status";
        clientWs.Cell(1, 2).Value = "Count";
        StyleHeader(clientWs.Range(1, 1, 1, 2));

        int r = 2;
        foreach (var b in clientBreakdown)
        {
            clientWs.Cell(r, 1).Value = b.Status;
            clientWs.Cell(r, 2).Value = b.Count;
            r++;
        }
        clientWs.Columns().AdjustToContents();

        // Project breakdown sheet
        var projectWs = workbook.Worksheets.Add("Project Status");
        projectWs.Cell(1, 1).Value = "Status";
        projectWs.Cell(1, 2).Value = "Count";
        StyleHeader(projectWs.Range(1, 1, 1, 2));

        r = 2;
        foreach (var b in projectBreakdown)
        {
            projectWs.Cell(r, 1).Value = b.Status;
            projectWs.Cell(r, 2).Value = b.Count;
            r++;
        }
        projectWs.Columns().AdjustToContents();

        return SaveToBytes(workbook);
    }

    private static void StyleHeader(IXLRange headerRange)
    {
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#2e7d32");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static byte[] SaveToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
