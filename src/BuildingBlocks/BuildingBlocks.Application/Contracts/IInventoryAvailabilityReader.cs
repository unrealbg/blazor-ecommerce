namespace BuildingBlocks.Application.Contracts;

public interface IInventoryAvailabilityReader
{
    Task<InventoryAvailabilitySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);

    Task<InventoryAvailabilitySnapshot?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}
