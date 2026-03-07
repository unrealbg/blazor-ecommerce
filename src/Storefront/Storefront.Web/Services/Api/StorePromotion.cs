namespace Storefront.Web.Services.Api;

public sealed record StorePromotion(
    Guid Id,
    string Name,
    string? Code,
    int Type,
    int Status,
    string? Description,
    int Priority,
    bool IsExclusive,
    bool AllowWithCoupons,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    int TimesUsedTotal,
    IReadOnlyCollection<StorePromotionScope> Scopes,
    IReadOnlyCollection<StorePromotionCondition> Conditions,
    IReadOnlyCollection<StorePromotionBenefit> Benefits,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
