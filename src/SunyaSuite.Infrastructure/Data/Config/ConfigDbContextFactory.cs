using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SunyaSuite.Infrastructure.Data.Config;

public class ConfigDbContextFactory : IDesignTimeDbContextFactory<ConfigDbContext>
{
    public ConfigDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ConfigDbContext>()
            .UseNpgsql("Host=localhost;Port=5433;Database=SunyaSuite_Config;Username=postgres;Password=postgres");

        return new ConfigDbContext(optionsBuilder.Options, TimeProvider.System);
    }
}
