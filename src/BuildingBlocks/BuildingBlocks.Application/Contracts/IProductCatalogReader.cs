namespace BuildingBlocks.Application.Contracts;

public interface IProductCatalogReader
{
    Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<ProductSnapshot?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, ProductSnapshot>> GetByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ProductSnapshot>> ListAllAsync(CancellationToken cancellationToken);
}
