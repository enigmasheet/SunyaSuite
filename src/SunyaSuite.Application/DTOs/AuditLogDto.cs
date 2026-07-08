namespace SunyaSuite.Application.DTOs;

public record AuditLogDto(
    Guid Id,
    string UserId,
    string Action,
    string EntityName,
    string EntityId,
    DateTime Timestamp,
    string Details);

public record AuditLogFilterDto(
    string? SearchTerm = null,
    string? Action = null,
    string? EntityName = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null);
