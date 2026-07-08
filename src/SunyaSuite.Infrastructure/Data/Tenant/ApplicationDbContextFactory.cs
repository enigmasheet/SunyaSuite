using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SunyaSuite.Infrastructure.Data.Tenant;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5433;Database=SunyaSuite;Username=postgres;Password=postgres");

        return new ApplicationDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}
