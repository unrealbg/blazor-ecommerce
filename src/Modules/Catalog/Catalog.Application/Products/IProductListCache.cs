using BuildingBlocks.Application.Contracts;

namespace Catalog.Application.Products;

public interface IProductListCache
{
    Task<IReadOnlyCollection<ProductSnapshot>?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(IReadOnlyCollection<ProductSnapshot> products, CancellationToken cancellationToken);

    Task InvalidateAsync(CancellationToken cancellationToken);
}
