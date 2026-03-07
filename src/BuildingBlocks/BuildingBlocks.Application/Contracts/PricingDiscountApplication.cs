namespace BuildingBlocks.Application.Contracts;

public sealed record PricingDiscountApplication(
    string SourceType,
    Guid SourceId,
    Guid PromotionId,
    Guid? CouponId,
    string ScopeType,
    Guid? TargetLineVariantId,
    string Description,
    decimal Amount,
    string Currency,
    string? Code = null);
