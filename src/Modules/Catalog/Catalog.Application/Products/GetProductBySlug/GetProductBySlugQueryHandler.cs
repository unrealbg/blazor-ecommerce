using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(
    IProductCatalogReader productCatalogReader)
    : IQueryHandler<GetProductBySlugQuery, ProductSnapshot?>
{
    public async Task<ProductSnapshot?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var products = await productCatalogReader.ListAllAsync(cancellationToken);
        return products.SingleOrDefault(product => product.Slug == request.Slug.Trim().ToLowerInvariant());
    }
}
