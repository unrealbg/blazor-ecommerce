using Catalog.Domain.Products;

namespace Catalog.Application.Products;

public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken);
}
