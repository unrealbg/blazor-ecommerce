using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProducts;

public sealed record GetProductsQuery : IQuery<IReadOnlyCollection<ProductSnapshot>>;
