using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullVariantPricingService : IVariantPricingService
{
    public Task<VariantPricingSnapshot?> GetVariantPricingAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<VariantPricingSnapshot?>(null);
    }

    public Task<IReadOnlyDictionary<Guid, VariantPricingSnapshot>> GetVariantPricingAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, VariantPricingSnapshot>>(
            new Dictionary<Guid, VariantPricingSnapshot>());
    }
}
