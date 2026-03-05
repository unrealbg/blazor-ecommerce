using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.CreateProduct;

public sealed record CreateProductCommand(string Name, string Currency, decimal Amount) : ICommand<Guid>;
