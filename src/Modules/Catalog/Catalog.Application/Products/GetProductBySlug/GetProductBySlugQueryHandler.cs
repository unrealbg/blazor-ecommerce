using BuildingBlocks.Application.Abstractions;

namespace Catalog.Application.Products.GetProductBySlug;

public sealed class GetProductBySlugQueryHandler(IProductRepository productRepository)
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

        return new ProductDto(
            product.Id,
            product.Slug,
            product.Name,
            product.Description,
            product.Brand,
            product.Sku,
            product.ImageUrl,
            product.IsInStock,
            product.CategorySlug,
            product.CategoryName,
            product.Price.Currency,
            product.Price.Amount,
            product.IsActive);
    }
}
