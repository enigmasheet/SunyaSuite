using SunyaSuite.Domain.Enums;
using SunyaSuite.Domain.Interfaces;

namespace SunyaSuite.Domain.Entities.Tenant;

public class Project : ICompanyScoped
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid ClientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly Deadline { get; set; }
    private int _progressPercent;
    public int ProgressPercent
    {
        get => _progressPercent;
        set => _progressPercent = Math.Clamp(value, 0, 100);
    }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Company CompanyInfo { get; set; } = null!;
    public Branch? BranchInfo { get; set; }
    public Client Client { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
