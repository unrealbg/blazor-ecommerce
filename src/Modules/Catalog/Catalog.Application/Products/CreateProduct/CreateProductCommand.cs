using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    string? Brand,
    string? Sku,
    string? ImageUrl,
    bool IsInStock,
    string? CategorySlug,
    string? CategoryName,
    string Currency,
    decimal Amount,
    bool IsActive) : ICommand<Guid>;
