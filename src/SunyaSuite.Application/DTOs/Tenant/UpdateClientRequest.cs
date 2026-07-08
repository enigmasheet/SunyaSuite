namespace SunyaSuite.Application.DTOs.Tenant;

public record UpdateClientRequest(
    Guid Id,
    string Name,
    string Email,
    string Company,
    string Phone,
    string Address,
    string? PanNumber);
