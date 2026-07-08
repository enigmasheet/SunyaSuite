namespace SunyaSuite.Application.DTOs.Tenant;

public record CreateClientRequest(
    string Name,
    string Email,
    string Company,
    string Phone,
    string Address,
    string? PanNumber);
