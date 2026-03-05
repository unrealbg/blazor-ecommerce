using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.GetProducts;

public sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductDto>>;
