namespace SunyaSuite.Application.DTOs;

public record PagedResult<T>(List<T> Items, int Total);
