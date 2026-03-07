namespace Pricing.Domain.Promotions;

public sealed record PromotionBenefitData(
    PromotionBenefitType BenefitType,
    decimal? ValueAmount,
    decimal? ValuePercent,
    decimal? MaxDiscountAmount,
    bool ApplyPerUnit);
