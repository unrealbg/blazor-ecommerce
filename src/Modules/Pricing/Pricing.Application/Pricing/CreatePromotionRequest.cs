using Pricing.Domain.Promotions;

namespace Pricing.Application.Pricing;

public sealed record CreatePromotionRequest(
    string Name,
    string? Code,
    PromotionType Type,
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
