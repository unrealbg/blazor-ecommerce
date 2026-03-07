using Pricing.Domain.Promotions;

namespace Pricing.Application.Pricing;

public sealed record UpdatePromotionRequest(
    string Name,
    string? Code,
    string? Description,
    int Priority,
    bool IsExclusive,
    bool AllowWithCoupons,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    IReadOnlyCollection<PromotionScopeData> Scopes,
    IReadOnlyCollection<PromotionConditionData> Conditions,
    IReadOnlyCollection<PromotionBenefitData> Benefits);
