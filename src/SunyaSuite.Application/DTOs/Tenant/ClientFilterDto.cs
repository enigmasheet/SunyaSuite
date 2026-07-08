using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public class ClientFilterDto
{
    public List<ClientStatus>? Statuses { get; set; }
    public DateOnly? RegisteredOnFrom { get; set; }
    public DateOnly? RegisteredOnTo { get; set; }
}
