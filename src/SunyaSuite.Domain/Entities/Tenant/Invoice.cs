using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Domain.Entities.Tenant;

public class Invoice
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ClientId { get; set; }
    public BillType BillType { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string DateBS { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; private set; }
    public string GrandTotalInWords { get; set; } = string.Empty;
    public bool IsAbbreviated { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string BuyerPan { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerPan { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public decimal AmountPaid { get; set; }
    public Guid FiscalYearId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectRemark { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public string? SellerLogoBase64 { get; set; }

    public Company CompanyInfo { get; set; } = null!;
    public Branch? BranchInfo { get; set; }
    public FiscalYear FiscalYearInfo { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public Project? Project { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<ReceiptInvoiceAllocation> ReceiptAllocations { get; set; } = new List<ReceiptInvoiceAllocation>();

    public void Recalculate(decimal vatRatePercentage = 13m)
    {
        Subtotal = Items.Sum(i => i.Amount);

        if (BillType == BillType.VatBill && !IsAbbreviated)
        {
            VatAmount = Math.Round(Subtotal * vatRatePercentage / 100m, 2);
            Total = Subtotal + VatAmount - DiscountAmount;
        }
        else
        {
            VatAmount = 0;
            Total = Subtotal - DiscountAmount;
        }
    }
}
