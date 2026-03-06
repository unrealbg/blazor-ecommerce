namespace BuildingBlocks.Application.Contracts;

public interface IInventoryAvailabilityReader
{
    Task<InventoryAvailabilitySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
