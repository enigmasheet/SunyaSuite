using SunyaSuite.Application.DTOs.Config;

namespace SunyaSuite.Application.Interfaces.Config;

public interface IInviteService
{
    Task<(List<InviteDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task<InviteDto> CreateAsync(string role, int? expiresInHours, string createdByUserId, CancellationToken ct = default);
    Task<InviteDto?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> ValidateInviteAsync(string code, CancellationToken ct = default);
    Task<(string Role, string Code)> ConsumeInviteAsync(string code, string usedByEmail, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
