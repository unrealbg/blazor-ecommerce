namespace BuildingBlocks.Application.Contracts;

public interface IProductCatalogReader
{
    Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductSnapshot>> ListAllAsync(CancellationToken cancellationToken);
}
