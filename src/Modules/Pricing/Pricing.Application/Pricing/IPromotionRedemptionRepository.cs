using Pricing.Domain.Redemptions;

namespace Pricing.Application.Pricing;

public interface IPromotionRedemptionRepository
{
    Task AddAsync(PromotionRedemption redemption, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(
        Guid orderId,
        Guid promotionId,
        Guid? couponId,
        CancellationToken cancellationToken);

    Task<int> CountPromotionRedemptionsAsync(
        Guid promotionId,
        string? customerId,
        CancellationToken cancellationToken);

    Task<int> CountCouponRedemptionsAsync(
        Guid couponId,
        string? customerId,
        CancellationToken cancellationToken);
}
