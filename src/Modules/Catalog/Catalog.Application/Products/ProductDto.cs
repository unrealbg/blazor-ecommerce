namespace Catalog.Application.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Currency,
    decimal Amount);
