using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(
    IProductRepository productRepository,
    IInventoryAvailabilityReader inventoryAvailabilityReader)
    : IQueryHandler<GetProductBySlugQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductBySlugQuery request, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetBySlugAsync(
            request.Slug.Trim().ToLowerInvariant(),
            cancellationToken);
        if (product is null)
        {
            return null;
        }

        var availability = await inventoryAvailabilityReader.GetByProductIdAsync(product.Id, cancellationToken);
        var isInStock = availability?.IsInStock ?? product.IsInStock;

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
            availability?.IsTracked ?? false,
            availability?.AllowBackorder ?? false,
            availability?.AvailableQuantity);
    }
}
