using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NepDate;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.DataSeeding;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;

        var seedSettings = sp.GetRequiredService<IOptions<SeedDataSettings>>().Value;

        await SeedConfigDatabaseAsync(sp, seedSettings);
        await SeedTenantDatabaseAsync(sp, seedSettings);
    }

    private static async Task SeedConfigDatabaseAsync(IServiceProvider sp, SeedDataSettings seedSettings)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] { RoleNames.SystemAdmin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Migrate old role name (Admin→SystemAdmin)
        var oldRoleMap = new Dictionary<string, string> { ["Admin"] = RoleNames.SystemAdmin };
        foreach (var (oldName, newName) in oldRoleMap)
        {
            if (await roleManager.RoleExistsAsync(oldName))
            {
                var oldRole = await roleManager.FindByNameAsync(oldName);
                if (oldRole is not null)
                {
                    var usersInOldRole = await userManager.GetUsersInRoleAsync(oldName);
                    foreach (var user in usersInOldRole)
                    {
                        await userManager.AddToRoleAsync(user, newName);
                        await userManager.RemoveFromRoleAsync(user, oldName);
                    }
                    var identityResult = await roleManager.DeleteAsync(oldRole);
                }
            }
        }

        var adminEmail = seedSettings.AdminEmail;
        ApplicationUser? admin;
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, seedSettings.AdminPassword);
            if (result.Succeeded)
            {
                var confirmToken = await userManager.GenerateEmailConfirmationTokenAsync(admin);
                await userManager.ConfirmEmailAsync(admin, confirmToken);
                await userManager.AddToRoleAsync(admin, RoleNames.SystemAdmin);
            }
        }
        else
        {
            admin = await userManager.FindByEmailAsync(adminEmail);
        }

        if (admin is null) return;

        var configFactory = sp.GetRequiredService<IDbContextFactory<ConfigDbContext>>();
        await using var configCtx = await configFactory.CreateDbContextAsync();

        var defaultOrg = await configCtx.Organizations
            .FirstOrDefaultAsync(o => o.Slug == "default");

        if (defaultOrg is null)
        {
            defaultOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Organization",
                Slug = "default",
                ConnectionString = null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            configCtx.Organizations.Add(defaultOrg);
            await configCtx.SaveChangesAsync();
        }

        var existingLink = await configCtx.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == admin.Id && ou.OrganizationId == defaultOrg.Id);

        if (existingLink is null)
        {
            configCtx.OrganizationUsers.Add(new OrganizationUser
            {
                Id = Guid.NewGuid(),
                OrganizationId = defaultOrg.Id,
                UserId = admin.Id,
                Role = OrgRoles.Owner,
                JoinedAt = DateTime.UtcNow
            });
            await configCtx.SaveChangesAsync();
        }
    }

    private static async Task SeedTenantDatabaseAsync(IServiceProvider sp, SeedDataSettings seedSettings)
    {
        var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        await using var context = await factory.CreateDbContextAsync();
        await SeedTenantDataAsync(context);

        await using var configCtx = sp.GetRequiredService<IDbContextFactory<ConfigDbContext>>().CreateDbContext();

        var defaultCompany = await context.Companies.FirstOrDefaultAsync(c => c.Slug == "default");
        var defaultBranch = defaultCompany is not null
            ? await context.Branches.FirstOrDefaultAsync(b => b.CompanyId == defaultCompany.Id && b.Slug == "main")
            : null;

        if (defaultCompany is not null)
        {
            var adminEmail = seedSettings.AdminEmail;
            var admin = await sp.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(adminEmail);

            if (admin is not null)
            {
                var defaultOrg = await configCtx.Organizations.FirstOrDefaultAsync(o => o.Slug == "default");
                if (defaultOrg is not null)
                {
                    var orgUser = await configCtx.OrganizationUsers
                        .FirstOrDefaultAsync(ou => ou.UserId == admin.Id && ou.OrganizationId == defaultOrg.Id);

                    if (orgUser is not null && orgUser.DefaultCompanyId is null)
                    {
                        orgUser.DefaultCompanyId = defaultCompany.Id;
                        orgUser.DefaultBranchId = defaultBranch?.Id;
                        await configCtx.SaveChangesAsync();
                    }
                }
            }
        }
    }

    private static async Task SeedTenantDataAsync(ApplicationDbContext context)
    {
        var company = await EnsureDefaultCompanyAsync(context);
        await SeedFiscalYearsAsync(context, company);
    }

    private static async Task<Company> EnsureDefaultCompanyAsync(ApplicationDbContext context)
    {
        var defaultCompany = await context.Companies
            .FirstOrDefaultAsync(c => c.Slug == "default");

        if (defaultCompany is null)
        {
            defaultCompany = new Company
            {
                Id = Guid.NewGuid(),
                Name = "Default Company",
                Slug = "default",
                Email = "admin@company.local",
                Phone = "9800000000",
                Address = "Kathmandu, Nepal",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            context.Companies.Add(defaultCompany);
            await context.SaveChangesAsync();
        }

        var defaultBranch = await context.Branches
            .FirstOrDefaultAsync(b => b.CompanyId == defaultCompany.Id && b.Slug == "main");

        if (defaultBranch is null)
        {
            context.Branches.Add(new Branch
            {
                Id = Guid.NewGuid(),
                CompanyId = defaultCompany.Id,
                Name = "Main Branch",
                Slug = "main",
                Address = "Kathmandu, Nepal",
                Phone = "9800000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        return defaultCompany;
    }

    private static async Task SeedFiscalYearsAsync(ApplicationDbContext context, Company company)
    {
        if (await context.FiscalYears.AnyAsync())
            return;

        var now = DateTime.UtcNow;
        var todayNepali = new NepaliDate(now);
        var fyYear = todayNepali.Month >= 4 ? todayNepali.Year : todayNepali.Year - 1;

        for (var y = fyYear - 1; y <= fyYear + 1; y++)
        {
            var startBS = new NepaliDate(y, 4, 1);
            var endBS = new NepaliDate(y + 1, 3, 30);
            context.FiscalYears.Add(new FiscalYear
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                YearName = $"{y}/{((y + 1) % 100):D2}",
                StartDateBS = startBS.ToString("yyyy/MM/dd"),
                EndDateBS = endBS.ToString("yyyy/MM/dd"),
                StartDateAD = DateOnly.FromDateTime(startBS.EnglishDate),
                EndDateAD = DateOnly.FromDateTime(endBS.EnglishDate),
                IsOpen = true,
                IsCurrent = y == fyYear,
                CreatedAt = now
            });
        }

        await context.SaveChangesAsync();
    }
}
