namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record CreateFiscalYearRequest(
    string YearName,
    string StartDateBS,
    string EndDateBS);
