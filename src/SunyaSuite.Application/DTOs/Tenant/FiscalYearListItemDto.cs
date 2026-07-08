namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record FiscalYearListItemDto(
    Guid Id,
    string YearName,
    string StartDateBS,
    string EndDateBS,
    bool IsOpen,
    bool IsCurrent);
