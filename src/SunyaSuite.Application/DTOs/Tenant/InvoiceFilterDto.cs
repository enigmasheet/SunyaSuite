using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public class InvoiceFilterDto
{
    public List<InvoiceStatus>? Statuses { get; set; }
    public DateOnly? IssueDateFrom { get; set; }
    public DateOnly? IssueDateTo { get; set; }
    public DateOnly? DueDateFrom { get; set; }
    public DateOnly? DueDateTo { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? FiscalYearId { get; set; }
}
