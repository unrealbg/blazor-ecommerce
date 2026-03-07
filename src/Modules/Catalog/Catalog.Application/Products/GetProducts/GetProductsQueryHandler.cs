using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductCatalogReader productCatalogReader,
    IProductListCache productListCache)
    : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductSnapshot>>
{
    public async Task<IReadOnlyCollection<ProductSnapshot>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await productListCache.GetAsync(cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var response = await productCatalogReader.ListAllAsync(cancellationToken);

        await productListCache.SetAsync(response, cancellationToken);
        return response;
    }
}
