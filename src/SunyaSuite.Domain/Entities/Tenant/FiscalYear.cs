using SunyaSuite.Domain.Interfaces;

namespace SunyaSuite.Domain.Entities.Tenant;

public class FiscalYear : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string YearName { get; set; } = string.Empty;
    public string StartDateBS { get; set; } = string.Empty;
    public string EndDateBS { get; set; } = string.Empty;
    public DateOnly StartDateAD { get; set; }
    public DateOnly EndDateAD { get; set; }
    public bool IsOpen { get; set; } = true;
    public bool IsCurrent { get; set; }
    public DateTime CreatedAt { get; set; }

    public Company CompanyInfo { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<MoneyReceipt> MoneyReceipts { get; set; } = new List<MoneyReceipt>();
}
