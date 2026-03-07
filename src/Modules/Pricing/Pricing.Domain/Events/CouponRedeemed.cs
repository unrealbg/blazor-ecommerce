using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Events;

public sealed record CouponRedeemed(
    Guid CouponId,
    string Code,
    Guid PromotionId,
    Guid OrderId,
    string? CustomerId,
    decimal DiscountAmount) : DomainEvent;
