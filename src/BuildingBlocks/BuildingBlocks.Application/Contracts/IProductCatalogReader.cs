namespace BuildingBlocks.Application.Contracts;

public interface IProductCatalogReader
{
    Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);
}
