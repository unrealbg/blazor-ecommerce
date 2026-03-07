namespace BuildingBlocks.Application.Contracts;

public interface IVariantPricingService
{
    Task<VariantPricingSnapshot?> GetVariantPricingAsync(Guid variantId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, VariantPricingSnapshot>> GetVariantPricingAsync(
        IReadOnlyCollection<Guid> variantIds,
        CancellationToken cancellationToken);
}
