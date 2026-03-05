namespace Catalog.Application.Products;

public interface IProductListCache
{
    Task<IReadOnlyCollection<ProductDto>?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(IReadOnlyCollection<ProductDto> products, CancellationToken cancellationToken);

    Task InvalidateAsync(CancellationToken cancellationToken);
}
