using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Domain.Entities.Tenant;

public class Client
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? PanNumber { get; set; }
    public DateOnly RegisteredOn { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.Green;
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company CompanyInfo { get; set; } = null!;
    public Branch? BranchInfo { get; set; }
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
