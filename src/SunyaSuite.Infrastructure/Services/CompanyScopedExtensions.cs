using SunyaSuite.Domain.Interfaces;

namespace SunyaSuite.Infrastructure.Services;

public static class CompanyScopedExtensions
{
    public static IQueryable<T> ForCompany<T>(this IQueryable<T> query, Guid companyId)
        where T : ICompanyScoped
        => query.Where(e => e.CompanyId == companyId);
}
