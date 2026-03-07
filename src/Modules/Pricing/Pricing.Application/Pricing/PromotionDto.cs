using Pricing.Domain.Promotions;

namespace Pricing.Application.Pricing;

public sealed record PromotionDto(
    Guid Id,
    string Name,
    string? Code,
    PromotionType Type,
    PromotionStatus Status,
    string? Description,
    int Priority,
    bool IsExclusive,
    bool AllowWithCoupons,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    int TimesUsedTotal,
    IReadOnlyCollection<PromotionScopeData> Scopes,
    IReadOnlyCollection<PromotionConditionData> Conditions,
    IReadOnlyCollection<PromotionBenefitData> Benefits,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
