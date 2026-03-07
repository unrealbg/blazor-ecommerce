using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Domain.Results;
using Pricing.Application.Pricing;
using Pricing.Domain.Redemptions;

namespace Pricing.Infrastructure.Pricing;

internal sealed class PricingRedemptionService(
    IPromotionRepository promotionRepository,
    ICouponRepository couponRepository,
    IPromotionRedemptionRepository promotionRedemptionRepository,
    IPricingUnitOfWork unitOfWork,
    IClock clock)
    : IPricingRedemptionService
{
    public async Task<Result> RegisterOrderRedemptionsAsync(
        Guid orderId,
        string? customerId,
        IReadOnlyCollection<PricingDiscountApplication> applications,
        CancellationToken cancellationToken)
    {
        var groups = applications
            .Where(application => application.PromotionId != Guid.Empty)
            .GroupBy(application => new { application.PromotionId, application.CouponId })
            .ToList();

        foreach (var group in groups)
        {
            if (await promotionRedemptionRepository.ExistsAsync(
                    orderId,
                    group.Key.PromotionId,
                    group.Key.CouponId,
                    cancellationToken))
            {
                continue;
            }

            var promotion = await promotionRepository.GetByIdAsync(group.Key.PromotionId, cancellationToken);
            if (promotion is null)
            {
                continue;
            }

            var coupon = group.Key.CouponId is null
                ? null
                : await couponRepository.GetByIdAsync(group.Key.CouponId.Value, cancellationToken);

            var discountAmount = decimal.Round(group.Sum(item => item.Amount), 2, MidpointRounding.AwayFromZero);
            var redemptionResult = PromotionRedemption.Create(
                promotion.Id,
                coupon?.Id,
                orderId,
                customerId,
                discountAmount,
                clock.UtcNow);
            if (redemptionResult.IsFailure)
            {
                return Result.Failure(redemptionResult.Error);
            }

            promotion.RegisterRedemption(coupon, orderId, customerId, discountAmount, clock.UtcNow);
            coupon?.RegisterRedemption(orderId, customerId, discountAmount, clock.UtcNow);
            await promotionRedemptionRepository.AddAsync(redemptionResult.Value, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
