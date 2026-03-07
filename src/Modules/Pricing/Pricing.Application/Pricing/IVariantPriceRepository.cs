using Pricing.Domain.VariantPrices;

namespace Pricing.Application.Pricing;

public interface IVariantPriceRepository
{
    Task AddAsync(VariantPrice variantPrice, CancellationToken cancellationToken);

    Task<VariantPrice?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<VariantPrice?> GetActiveForVariantAsync(
        Guid priceListId,
        Guid variantId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, VariantPrice>> GetActiveForVariantsAsync(
        Guid priceListId,
        IReadOnlyCollection<Guid> variantIds,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
