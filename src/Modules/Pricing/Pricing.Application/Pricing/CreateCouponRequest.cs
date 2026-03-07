namespace Pricing.Application.Pricing;

public sealed record CreateCouponRequest(
    string Code,
    string? Description,
    Guid PromotionId,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer);
