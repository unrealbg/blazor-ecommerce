using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    string? Description,
    string Currency,
    decimal Amount,
    bool IsActive) : ICommand<Guid>;
