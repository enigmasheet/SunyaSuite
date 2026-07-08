namespace SunyaSuite.Application.DTOs.Tenant;

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? HsCode { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public decimal Amount => Quantity * UnitPrice;
}
