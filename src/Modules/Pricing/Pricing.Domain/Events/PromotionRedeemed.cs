using BuildingBlocks.Domain.Primitives;

namespace Pricing.Domain.Events;

public sealed record PromotionRedeemed(
    Guid PromotionId,
    Guid? CouponId,
    Guid OrderId,
    string? CustomerId,
    decimal DiscountAmount) : DomainEvent;
