namespace Storefront.Web.Services.Api;

public sealed record StorePromotionRequest(
    string Name,
    string? Code,
    int Type,
    string? Description,
    int Priority,
    bool IsExclusive,
    bool AllowWithCoupons,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    IReadOnlyCollection<StorePromotionScope> Scopes,
    IReadOnlyCollection<StorePromotionCondition> Conditions,
    IReadOnlyCollection<StorePromotionBenefit> Benefits);
