namespace Catalog.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    string Currency,
    decimal Amount,
    bool IsActive);
