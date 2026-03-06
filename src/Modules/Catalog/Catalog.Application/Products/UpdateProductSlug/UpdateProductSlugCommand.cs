using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.UpdateProductSlug;

public sealed record UpdateProductSlugCommand(Guid ProductId, string Slug) : ICommand<string>;
