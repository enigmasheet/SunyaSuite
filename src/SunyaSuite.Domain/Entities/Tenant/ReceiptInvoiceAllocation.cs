namespace SunyaSuite.Domain.Entities.Tenant;

public class ReceiptInvoiceAllocation
{
    public Guid Id { get; set; }
    public Guid MoneyReceiptId { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal AllocatedAmount { get; set; }

    public MoneyReceipt MoneyReceipt { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
}
