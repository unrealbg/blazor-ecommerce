using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<Product?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingProductId, CancellationToken cancellationToken);

    Task<bool> SkuExistsAsync(string sku, Guid? excludingVariantId, CancellationToken cancellationToken);
}
