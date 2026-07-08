using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;
using System.Security.Cryptography;

namespace SunyaSuite.Infrastructure.Services.Config;

public class InviteService : IInviteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<InviteSettings> _settings;
    private readonly ITenantContext _tenantContext;

    public InviteService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider,
        IOptions<InviteSettings> settings,
        ITenantContext tenantContext)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
        _settings = settings;
        _tenantContext = tenantContext;
    }

    public async Task<(List<InviteDto> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Invites.AsQueryable();

        if (_tenantContext.CompanyId.HasValue)
            query = query.Where(i => i.CompanyId == _tenantContext.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(i => i.Code.Contains(searchTerm) || i.UsedByEmail!.Contains(searchTerm));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InviteDto
            {
                Id = i.Id,
                Code = i.Code,
                Role = i.Role,
                IsUsed = i.IsUsed,
                CreatedAt = i.CreatedAt,
                ExpiresAt = i.ExpiresAt,
                UsedByEmail = i.UsedByEmail,
                UsedAt = i.UsedAt
            })
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<InviteDto> CreateAsync(string role, int? expiresInHours, string createdByUserId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var code = await GenerateUniqueCodeAsync(context, ct);
        var expiryHours = expiresInHours ?? _settings.Value.DefaultExpirationHours;

        var invite = new Invite
        {
            Id = Guid.NewGuid(),
            CompanyId = _tenantContext.CompanyId ?? Guid.Empty,
            Code = code,
            Role = role,
            IsUsed = false,
            CreatedAt = now,
            ExpiresAt = now.AddHours(expiryHours),
        };

        context.Invites.Add(invite);
        await context.SaveChangesAsync(ct);

        return MapToDto(invite);
    }

    public async Task<InviteDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var invite = await context.Invites.FirstOrDefaultAsync(i => i.Code == code, ct);
        return invite is null ? null : MapToDto(invite);
    }

    public async Task<bool> ValidateInviteAsync(string code, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var invite = await context.Invites.FirstOrDefaultAsync(i => i.Code == code, ct);
        if (invite is null) return false;
        if (invite.IsUsed) return false;
        if (_timeProvider.GetUtcNow().UtcDateTime > invite.ExpiresAt) return false;
        return true;
    }

    public async Task<(string Role, string Code)> ConsumeInviteAsync(string code, string usedByEmail, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var invite = await context.Invites.FirstOrDefaultAsync(i => i.Code == code, ct)
            ?? throw new InvalidOperationException("Invalid invite code.");

        if (invite.IsUsed)
            throw new InvalidOperationException("Invite code has already been used.");
        if (_timeProvider.GetUtcNow().UtcDateTime > invite.ExpiresAt)
            throw new InvalidOperationException("Invite code has expired.");

        invite.IsUsed = true;
        invite.UsedByEmail = usedByEmail;
        invite.UsedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await context.SaveChangesAsync(ct);

        return (invite.Role, invite.Code);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var invite = await context.Invites.FindAsync([id], ct);
        if (invite is not null)
        {
            context.Invites.Remove(invite);
            await context.SaveChangesAsync(ct);
        }
    }

    private static async Task<string> GenerateUniqueCodeAsync(ApplicationDbContext context, CancellationToken ct)
    {
        string code;
        do
        {
            code = GenerateCode();
        }
        while (await context.Invites.AnyAsync(i => i.Code == code, ct));
        return code;
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var data = new byte[6];
        RandomNumberGenerator.Fill(data);
        var result = new char[8];
        result[0] = 'I';
        result[1] = 'N';
        result[2] = 'V';
        result[3] = '-';
        for (int i = 0; i < 4; i++)
            result[4 + i] = chars[data[i] % chars.Length];
        return new string(result);
    }

    private static InviteDto MapToDto(Invite invite) => new()
    {
        Id = invite.Id,
        Code = invite.Code,
        Role = invite.Role,
        IsUsed = invite.IsUsed,
        CreatedAt = invite.CreatedAt,
        ExpiresAt = invite.ExpiresAt,
        UsedByEmail = invite.UsedByEmail,
        UsedAt = invite.UsedAt
    };
}
