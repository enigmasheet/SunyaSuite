using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IClientStatusCalculator
{
    ClientStatus Calculate(IEnumerable<Invoice> invoices);
}
