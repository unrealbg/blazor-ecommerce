using Pricing.Domain.Coupons;

namespace Pricing.Application.Pricing;

public sealed record CouponDto(
    Guid Id,
    string Code,
    string? Description,
    Guid PromotionId,
    CouponStatus Status,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer,
    int TimesUsedTotal,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
