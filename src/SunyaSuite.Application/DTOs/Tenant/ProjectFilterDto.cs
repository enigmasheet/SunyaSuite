using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public class ProjectFilterDto
{
    public List<ProjectStatus>? Statuses { get; set; }
    public DateOnly? DeadlineFrom { get; set; }
    public DateOnly? DeadlineTo { get; set; }
    public Guid? ClientId { get; set; }
}
