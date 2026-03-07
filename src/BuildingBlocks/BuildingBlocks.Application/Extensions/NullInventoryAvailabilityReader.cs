using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullInventoryAvailabilityReader : IInventoryAvailabilityReader
{
    public Task<InventoryAvailabilitySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Task.FromResult<InventoryAvailabilitySnapshot?>(null);
    }

    public Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByProductIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>>(
            new Dictionary<Guid, InventoryAvailabilitySnapshot>());
    }

    public Task<InventoryAvailabilitySnapshot?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<InventoryAvailabilitySnapshot?>(null);
    }

    public Task<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>> GetByVariantIdsAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, InventoryAvailabilitySnapshot>>(
            new Dictionary<Guid, InventoryAvailabilitySnapshot>());
    }
}
