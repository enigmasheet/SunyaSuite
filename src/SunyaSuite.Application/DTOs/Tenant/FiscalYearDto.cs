namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record FiscalYearDto(
    Guid Id,
    string YearName,
    string StartDateBS,
    string EndDateBS,
    DateOnly StartDateAD,
    DateOnly EndDateAD,
    bool IsOpen,
    bool IsCurrent);
