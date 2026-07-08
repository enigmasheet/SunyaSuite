namespace SunyaSuite.Domain.Entities.Tenant;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? HsCode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? ProjectId { get; set; }
    public decimal Amount => Quantity * UnitPrice;

    public Invoice Invoice { get; set; } = null!;
    public Project? Project { get; set; }
}
