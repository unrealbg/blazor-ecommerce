using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken);
}
