using Pricing.Domain.Promotions;

namespace Pricing.Application.Pricing;

public interface IPromotionRepository
{
    Task AddAsync(Promotion promotion, CancellationToken cancellationToken);

    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Promotion>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Promotion>> ListActiveAsync(DateTime utcNow, CancellationToken cancellationToken);
}
