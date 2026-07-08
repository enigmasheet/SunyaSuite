using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using SunyaSuite.Infrastructure.DataSeeding;
using System.Text.RegularExpressions;

namespace SunyaSuite.Infrastructure.Services.Config;

public class OrganizationService : IOrganizationService
{
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;
    private readonly DatabaseSettings _databaseSettings;
    private readonly TimeProvider _timeProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public OrganizationService(
        IDbContextFactory<ConfigDbContext> configFactory,
        IOptions<DatabaseSettings> databaseSettings,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager)
    {
        _configFactory = configFactory;
        _databaseSettings = databaseSettings.Value;
        _timeProvider = timeProvider;
        _userManager = userManager;
    }

    public async Task<List<OrganizationDto>> GetMyOrganizationsAsync(string userId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        return await configDb.OrganizationUsers
            .AsNoTracking()
            .Where(ou => ou.UserId == userId)
            .Select(ou => new OrganizationDto
            {
                Id = ou.Organization.Id,
                Name = ou.Organization.Name,
                Slug = ou.Organization.Slug,
                ConnectionString = ou.Organization.ConnectionString,
                Role = ou.Role
            })
            .ToListAsync();
    }

    public async Task<List<OrganizationDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);
        return await configDb.Organizations
            .AsNoTracking()
            .OrderBy(o => o.Name)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                ConnectionString = o.ConnectionString,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<(List<OrganizationDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);
        var query = configDb.Organizations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(o =>
                o.Name.ToLower().Contains(term) ||
                o.Slug.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                ConnectionString = o.ConnectionString,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);
        return await configDb.Organizations
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new OrganizationDto
            {
                Id = o.Id,
                Name = o.Name,
                Slug = o.Slug,
                ConnectionString = o.ConnectionString,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedAt
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<OrganizationUserDto>> GetUserOrganizationsAsync(string userId, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);
        return await configDb.OrganizationUsers
            .AsNoTracking()
            .Where(ou => ou.UserId == userId)
            .Select(ou => new OrganizationUserDto(
                ou.Id,
                ou.OrganizationId,
                ou.Organization.Name,
                ou.Organization.Slug,
                ou.UserId,
                ou.Role,
                ou.JoinedAt,
                ou.DefaultCompanyId,
                ou.DefaultBranchId))
            .ToListAsync(ct);
    }

    public async Task AssignToOrganizationAsync(string userId, Guid organizationId, string role, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);

        var exists = await configDb.OrganizationUsers
            .AnyAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId, ct);
        if (exists)
            throw new InvalidOperationException("User is already a member of this organization.");

        var orgExists = await configDb.Organizations.AnyAsync(o => o.Id == organizationId, ct);
        if (!orgExists)
            throw new KeyNotFoundException("Organization not found.");

        configDb.OrganizationUsers.Add(new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            Role = role,
            JoinedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        await configDb.SaveChangesAsync(ct);
    }

    public async Task UpdateOrganizationRoleAsync(string userId, Guid organizationId, string role, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);

        var membership = await configDb.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId, ct);
        if (membership is null)
            throw new KeyNotFoundException("User is not a member of this organization.");

        membership.Role = role;
        await configDb.SaveChangesAsync(ct);
    }

    public async Task RemoveFromOrganizationAsync(string userId, Guid organizationId, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);

        var membership = await configDb.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.UserId == userId && ou.OrganizationId == organizationId, ct);
        if (membership is not null)
        {
            configDb.OrganizationUsers.Remove(membership);
            await configDb.SaveChangesAsync(ct);
        }
    }

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, string adminUserId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        if (await configDb.Organizations.AnyAsync(o => o.Slug == request.Slug))
            throw new InvalidOperationException($"Organization with slug '{request.Slug}' already exists.");

        if (await configDb.Users.AnyAsync(u => u.Email == request.OwnerEmail))
            throw new InvalidOperationException($"User with email '{request.OwnerEmail}' already exists.");

        string? connectionString = null;
        string? createdDbName = null;
        if (!string.IsNullOrEmpty(request.DatabaseName))
        {
            var templateConnStr = _databaseSettings.TemplateConnection
                ?? throw new InvalidOperationException("Template connection string is not configured.");

            var templateDbName = ExtractDatabaseName(templateConnStr);
            var dbName = SanitizeDatabaseName(request.DatabaseName);
            connectionString = BuildConnectionString(templateConnStr, dbName);
            createdDbName = dbName;

            await CreateDatabaseFromTemplateAsync(templateConnStr, templateDbName, dbName);
        }

        var ownerUser = new ApplicationUser
        {
            UserName = request.OwnerEmail,
            Email = request.OwnerEmail,
            FirstName = request.OwnerFirstName,
            LastName = request.OwnerLastName,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        var createResult = await _userManager.CreateAsync(ownerUser, request.OwnerPassword);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(ownerUser);
        await _userManager.ConfirmEmailAsync(ownerUser, confirmToken);

        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            ConnectionString = connectionString,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        configDb.Organizations.Add(org);

        configDb.OrganizationUsers.Add(new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = ownerUser.Id,
            Role = OrgRoles.Owner,
            JoinedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        configDb.OrganizationUsers.Add(new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            UserId = adminUserId,
            Role = OrgRoles.OrgAdmin,
            JoinedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        try
        {
            await configDb.SaveChangesAsync();
        }
        catch
        {
            await _userManager.DeleteAsync(ownerUser);
            if (createdDbName is not null)
                await DropDatabaseIfExistsAsync(connectionString!);
            throw;
        }

        if (connectionString is not null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));

            await using var tenantDb = new ApplicationDbContext(optionsBuilder.Options, _timeProvider);
            await tenantDb.Database.MigrateAsync();
            await SeedData.SeedTenantDataAsync(tenantDb);
        }

        return new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            ConnectionString = org.ConnectionString,
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt,
            Role = OrgRoles.Owner
        };
    }

    public async Task<List<OrganizationUserDto>> GetOrgUsersAsync(Guid organizationId, CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);
        return await configDb.OrganizationUsers
            .AsNoTracking()
            .Where(ou => ou.OrganizationId == organizationId)
            .Select(ou => new OrganizationUserDto(
                ou.Id,
                ou.OrganizationId,
                ou.Organization.Name,
                ou.Organization.Slug,
                ou.UserId,
                ou.Role,
                ou.JoinedAt,
                ou.DefaultCompanyId,
                ou.DefaultBranchId))
            .ToListAsync(ct);
    }

    public async Task<UserDto> CreateUserForOrganizationAsync(Guid organizationId, CreateOrgUserRequest request, string adminUserId)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        if (request.Roles.Count > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(user, request.Roles);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.ConfirmEmailAsync(user, confirmToken);

        await using var configDb = await _configFactory.CreateDbContextAsync();

        var orgExists = await configDb.Organizations.AnyAsync(o => o.Id == organizationId);
        if (!orgExists)
            throw new KeyNotFoundException("Organization not found.");

        configDb.OrganizationUsers.Add(new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = user.Id,
            Role = request.OrgRole,
            JoinedAt = _timeProvider.GetUtcNow().UtcDateTime
        });

        await configDb.SaveChangesAsync();

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return new UserDto(user.Id, user.Email, user.UserName, user.FirstName, user.LastName, roles, user.CreatedAt, user.Preference);
    }

    public async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var org = await configDb.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org is null)
            throw new KeyNotFoundException("Organization not found.");

        if (await configDb.Organizations.AnyAsync(o => o.Slug == request.Slug && o.Id != id))
            throw new InvalidOperationException($"Organization with slug '{request.Slug}' already exists.");

        org.Name = request.Name;
        org.Slug = request.Slug;

        await configDb.SaveChangesAsync();

        return new OrganizationDto
        {
            Id = org.Id,
            Name = org.Name,
            Slug = org.Slug,
            ConnectionString = org.ConnectionString,
            IsActive = org.IsActive,
            CreatedAt = org.CreatedAt
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var org = await configDb.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org is null)
            throw new KeyNotFoundException("Organization not found.");

        if (org.DeletedAt is not null)
            throw new InvalidOperationException("Organization is already deleted.");

        org.IsActive = false;
        org.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await configDb.SaveChangesAsync();
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var org = await configDb.Organizations.FirstOrDefaultAsync(o => o.Id == id);
        if (org is null)
            throw new KeyNotFoundException("Organization not found.");

        org.IsActive = !org.IsActive;

        await configDb.SaveChangesAsync();
    }

    public async Task UpdateOrgUserDefaultsAsync(Guid orgId, string userId, Guid? companyId, Guid? branchId)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var membership = await configDb.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == orgId && ou.UserId == userId);
        if (membership is null)
            throw new KeyNotFoundException("User is not a member of this organization.");

        membership.DefaultCompanyId = companyId;
        membership.DefaultBranchId = branchId;

        await configDb.SaveChangesAsync();
    }

    public async Task UpdateOrgUserRoleAsync(Guid orgId, string userId, string role)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var membership = await configDb.OrganizationUsers
            .FirstOrDefaultAsync(ou => ou.OrganizationId == orgId && ou.UserId == userId);
        if (membership is null)
            throw new KeyNotFoundException("User is not a member of this organization.");

        membership.Role = role;

        await configDb.SaveChangesAsync();
    }

    private static string SanitizeDatabaseName(string name)
    {
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");
        return sanitized.Length > 63 ? sanitized[..63] : sanitized;
    }

    private static string BuildConnectionString(string baseConnectionString, string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = databaseName
        };
        return builder.ConnectionString;
    }

    private static string ExtractDatabaseName(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return builder.Database!;
    }

    private static async Task CreateDatabaseFromTemplateAsync(string baseConnectionString, string templateDbName, string databaseName)
    {
        var masterBuilder = new NpgsqlConnectionStringBuilder(baseConnectionString)
        {
            Database = "postgres"
        };

        await using var conn = new NpgsqlConnection(masterBuilder.ConnectionString);
        await conn.OpenAsync();

        await using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(1) FROM pg_database WHERE datname = @name";
        checkCmd.Parameters.AddWithValue("name", databaseName);
        if ((long)(await checkCmd.ExecuteScalarAsync())! > 0)
            return;

        await using var termCmd = conn.CreateCommand();
        termCmd.CommandText = @"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = @template
              AND pid <> pg_backend_pid();";
        termCmd.Parameters.AddWithValue("template", templateDbName);
        await termCmd.ExecuteNonQueryAsync();

        await using var createCmd = conn.CreateCommand();
        createCmd.CommandText = $@"CREATE DATABASE ""{databaseName}"" WITH TEMPLATE ""{templateDbName}"" ENCODING 'UTF8'";
        await createCmd.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseIfExistsAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var dbName = builder.Database!;
        builder.Database = "postgres";

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        await using var termCmd = conn.CreateCommand();
        termCmd.CommandText = @"
            SELECT pg_terminate_backend(pg_stat_activity.pid)
            FROM pg_stat_activity
            WHERE pg_stat_activity.datname = @name
              AND pid <> pg_backend_pid();";
        termCmd.Parameters.AddWithValue("name", dbName);
        await termCmd.ExecuteNonQueryAsync();

        await using var dropCmd = conn.CreateCommand();
        dropCmd.CommandText = $@"DROP DATABASE IF EXISTS ""{dbName}""";
        await dropCmd.ExecuteNonQueryAsync();
    }
}
