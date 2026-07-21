using SunyaSuite.Domain.Enums;
using SunyaSuite.Domain.Interfaces;

namespace SunyaSuite.Domain.Entities.Tenant;

public class MoneyReceipt : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateOnly DateAD { get; set; }
    public string DateBS { get; set; } = string.Empty;
    public string ReceivedFromName { get; set; } = string.Empty;
    public string? ReceivedFromPan { get; set; }
    public string? ReceivedFromAddress { get; set; }
    public decimal AmountReceived { get; set; }
    public string AmountInWords { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string? ReferenceNo { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid FiscalYearId { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string? SellerLogoBase64 { get; set; }

    public Company CompanyInfo { get; set; } = null!;
    public Branch? BranchInfo { get; set; }
    public FiscalYear FiscalYearInfo { get; set; } = null!;
    public ICollection<ReceiptInvoiceAllocation> Allocations { get; set; } = new List<ReceiptInvoiceAllocation>();
}
