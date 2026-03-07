using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.Redemptions;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PromotionRedemptionRepository(PricingDbContext dbContext) : IPromotionRedemptionRepository
{
    public Task AddAsync(PromotionRedemption redemption, CancellationToken cancellationToken)
    {
        return dbContext.PromotionRedemptions.AddAsync(redemption, cancellationToken).AsTask();
    }

    public Task<bool> ExistsAsync(
        Guid orderId,
        Guid promotionId,
        Guid? couponId,
        CancellationToken cancellationToken)
    {
        return dbContext.PromotionRedemptions.AnyAsync(
            redemption =>
                redemption.OrderId == orderId &&
                redemption.PromotionId == promotionId &&
                redemption.CouponId == couponId,
            cancellationToken);
    }

    public Task<int> CountPromotionRedemptionsAsync(
        Guid promotionId,
        string? customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.PromotionRedemptions.CountAsync(
            redemption =>
                redemption.PromotionId == promotionId &&
                redemption.CustomerId == customerId,
            cancellationToken);
    }

    public Task<int> CountCouponRedemptionsAsync(
        Guid couponId,
        string? customerId,
        CancellationToken cancellationToken)
    {
        return dbContext.PromotionRedemptions.CountAsync(
            redemption =>
                redemption.CouponId == couponId &&
                redemption.CustomerId == customerId,
            cancellationToken);
    }
}
