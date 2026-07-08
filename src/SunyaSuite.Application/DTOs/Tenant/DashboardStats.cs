namespace SunyaSuite.Application.DTOs.Tenant;

public record DashboardStats
{
    public int TotalClients { get; init; }
    public int ActiveProjects { get; init; }
    public int OverdueInvoices { get; init; }
    public int OutstandingInvoices { get; init; }
    public int PaidInvoices { get; init; }
    public decimal RevenueThisMonth { get; init; }
    public decimal RevenueTotal { get; init; }
    public List<StatusBreakdown> ClientStatusBreakdown { get; init; } = [];
    public List<StatusBreakdown> ProjectStatusBreakdown { get; init; } = [];
    public List<StatusBreakdown> InvoiceStatusBreakdown { get; init; } = [];
    public List<MonthlyRevenueDto> MonthlyRevenue { get; init; } = [];
}

public record StatusBreakdown(string Status, int Count);

public record RecentInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    decimal Total,
    DateOnly DueDate,
    string Status);
