using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductRepository productRepository,
    IProductListCache productListCache)
    : IQueryHandler<GetProductsQuery, IReadOnlyCollection<ProductDto>>
{
    public async Task<IReadOnlyCollection<ProductDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var cached = await productListCache.GetAsync(cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var products = await productRepository.ListAsync(cancellationToken);

        var response = products
            .Select(product => new ProductDto(
                product.Id,
                product.Name,
                product.Description,
                product.Price.Currency,
                product.Price.Amount,
                product.IsActive))
            .ToList();

        await productListCache.SetAsync(response, cancellationToken);
        return response;
    }
}
