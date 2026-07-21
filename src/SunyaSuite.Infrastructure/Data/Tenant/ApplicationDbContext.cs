using Microsoft.EntityFrameworkCore;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant;

public class ApplicationDbContext : DbContext
{
    private readonly TimeProvider _timeProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, TimeProvider timeProvider) : base(options)
    {
        _timeProvider = timeProvider;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MoneyReceipt> MoneyReceipts => Set<MoneyReceipt>();
    public DbSet<ReceiptInvoiceAllocation> ReceiptInvoiceAllocations => Set<ReceiptInvoiceAllocation>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<FiscalYear> FiscalYears => Set<FiscalYear>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly,
            t => t.Namespace is not null &&
                 t.Namespace.StartsWith("SunyaSuite.Infrastructure.Data.Tenant.Configurations"));

        builder.HasSequence<long>("InvoiceSequence")
            .StartsAt(1)
            .IncrementsBy(1);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added)
            {
                if (entry.Entity is Client client && client.CreatedAt == default)
                    client.CreatedAt = utcNow;
                if (entry.Entity is AuditLog log && log.Timestamp == default)
                    log.Timestamp = utcNow;
                if (entry.Entity is FiscalYear fy && fy.CreatedAt == default)
                    fy.CreatedAt = utcNow;
                if (entry.Entity is Company company && company.CreatedAt == default)
                    company.CreatedAt = utcNow;
                if (entry.Entity is Branch branch && branch.CreatedAt == default)
                    branch.CreatedAt = utcNow;
            }

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                {
                    property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}
