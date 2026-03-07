namespace Pricing.Application.Pricing;

public sealed record UpdateCouponRequest(
    string? Description,
    DateTime? StartAtUtc,
    DateTime? EndAtUtc,
    int? UsageLimitTotal,
    int? UsageLimitPerCustomer);
