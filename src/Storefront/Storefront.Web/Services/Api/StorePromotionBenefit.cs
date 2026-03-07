namespace Storefront.Web.Services.Api;

public sealed record StorePromotionBenefit(
    int BenefitType,
    decimal? ValueAmount,
    decimal? ValuePercent,
    decimal? MaxDiscountAmount,
    bool ApplyPerUnit);
