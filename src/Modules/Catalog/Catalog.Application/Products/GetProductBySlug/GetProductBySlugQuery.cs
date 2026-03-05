using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IQuery<ProductDto?>;
