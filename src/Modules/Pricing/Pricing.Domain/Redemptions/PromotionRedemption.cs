using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Pricing.Domain.Redemptions;

public sealed class PromotionRedemption : AggregateRoot<Guid>
{
    private PromotionRedemption()
    {
    }

    private PromotionRedemption(
        Guid id,
        Guid promotionId,
        Guid? couponId,
        Guid orderId,
        string? customerId,
        decimal discountAmount,
        DateTime createdAtUtc)
    {
        Id = id;
        PromotionId = promotionId;
        CouponId = couponId;
        OrderId = orderId;
        CustomerId = customerId;
        DiscountAmount = discountAmount;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid PromotionId { get; private set; }

    public Guid? CouponId { get; private set; }

    public Guid OrderId { get; private set; }

    public string? CustomerId { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Result<PromotionRedemption> Create(
        Guid promotionId,
        Guid? couponId,
        Guid orderId,
        string? customerId,
        decimal discountAmount,
        DateTime createdAtUtc)
    {
        if (promotionId == Guid.Empty)
        {
            return Result<PromotionRedemption>.Failure(new Error(
                "pricing.redemption.promotion.required",
                "Promotion id is required."));
        }

        if (orderId == Guid.Empty)
        {
            return Result<PromotionRedemption>.Failure(new Error(
                "pricing.redemption.order.required",
                "Order id is required."));
        }

        if (discountAmount < 0m)
        {
            return Result<PromotionRedemption>.Failure(new Error(
                "pricing.redemption.discount.invalid",
                "Discount amount cannot be negative."));
        }

        return Result<PromotionRedemption>.Success(new PromotionRedemption(
            Guid.NewGuid(),
            promotionId,
            couponId,
            orderId,
            string.IsNullOrWhiteSpace(customerId) ? null : customerId.Trim(),
            decimal.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
            createdAtUtc));
    }
}
