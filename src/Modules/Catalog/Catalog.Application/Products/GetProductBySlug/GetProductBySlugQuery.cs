using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed record GetProductBySlugQuery(string Slug) : IQuery<ProductSnapshot?>;
