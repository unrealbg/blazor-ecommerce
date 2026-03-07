using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Pricing.Domain.Promotions;

public sealed class PromotionBenefit : Entity<Guid>
{
    private PromotionBenefit()
    {
    }

    private PromotionBenefit(
        Guid id,
        Guid promotionId,
        PromotionBenefitType benefitType,
        decimal? valueAmount,
        decimal? valuePercent,
        decimal? maxDiscountAmount,
        bool applyPerUnit)
    {
        Id = id;
        PromotionId = promotionId;
        BenefitType = benefitType;
        ValueAmount = valueAmount;
        ValuePercent = valuePercent;
        MaxDiscountAmount = maxDiscountAmount;
        ApplyPerUnit = applyPerUnit;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid PromotionId { get; private set; }

    public PromotionBenefitType BenefitType { get; private set; }

    public decimal? ValueAmount { get; private set; }

    public decimal? ValuePercent { get; private set; }

    public decimal? MaxDiscountAmount { get; private set; }

    public bool ApplyPerUnit { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    internal static PromotionBenefit Create(
        Guid promotionId,
        PromotionBenefitType benefitType,
        decimal? valueAmount,
        decimal? valuePercent,
        decimal? maxDiscountAmount,
        bool applyPerUnit)
    {
        var validation = Validate(benefitType, valueAmount, valuePercent, maxDiscountAmount);
        if (validation.IsFailure)
        {
            throw new InvalidOperationException(validation.Error.Message);
        }

        return new PromotionBenefit(
            Guid.NewGuid(),
            promotionId,
            benefitType,
            valueAmount,
            valuePercent,
            maxDiscountAmount,
            applyPerUnit);
    }

    private static Result Validate(
        PromotionBenefitType benefitType,
        decimal? valueAmount,
        decimal? valuePercent,
        decimal? maxDiscountAmount)
    {
        if (maxDiscountAmount is < 0m)
        {
            return Result.Failure(new Error(
                "pricing.promotion.max_discount.invalid",
                "Maximum discount amount cannot be negative."));
        }

        return benefitType switch
        {
            PromotionBenefitType.PercentageOff when valuePercent is null or <= 0m or > 100m =>
                Result.Failure(new Error("pricing.promotion.percent.invalid", "Percentage discount must be between 0 and 100.")),
            PromotionBenefitType.FixedAmountOff when valueAmount is null or <= 0m =>
                Result.Failure(new Error("pricing.promotion.amount.invalid", "Fixed discount amount must be greater than zero.")),
            PromotionBenefitType.FixedPrice when valueAmount is null or < 0m =>
                Result.Failure(new Error("pricing.promotion.fixed_price.invalid", "Fixed price must be zero or greater.")),
            PromotionBenefitType.FreeShipping => Result.Success(),
            _ => Result.Success(),
        };
    }
}
