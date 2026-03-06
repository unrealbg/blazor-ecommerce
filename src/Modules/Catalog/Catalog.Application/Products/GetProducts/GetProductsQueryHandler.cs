using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProducts;

public sealed class GetProductsQueryHandler(
    IProductRepository productRepository,
    IProductListCache productListCache,
    IInventoryAvailabilityReader inventoryAvailabilityReader)
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
        var availabilityByProduct = await inventoryAvailabilityReader.GetByProductIdsAsync(
            products.Select(product => product.Id).ToArray(),
            cancellationToken);

        var response = products
            .Select(product =>
            {
                var hasAvailability = availabilityByProduct.TryGetValue(product.Id, out var availability);
                var isTracked = hasAvailability && availability!.IsTracked;
                var allowBackorder = hasAvailability && availability!.AllowBackorder;
                var availableQuantity = hasAvailability ? (int?)availability!.AvailableQuantity : null;
                var isInStock = hasAvailability ? availability!.IsInStock : product.IsInStock;

                return new ProductDto(
                    product.Id,
                    product.Slug,
                    product.Name,
                    product.Description,
                    product.Brand,
                    product.Sku,
                    product.ImageUrl,
                    isInStock,
                    product.CategorySlug,
                    product.CategoryName,
                    product.Price.Currency,
                    product.Price.Amount,
                    product.IsActive,
                    isTracked,
                    allowBackorder,
                    availableQuantity);
            })
            .ToList();

        await productListCache.SetAsync(response, cancellationToken);
        return response;
    }
}
