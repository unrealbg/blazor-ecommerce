namespace BuildingBlocks.Application.Contracts;

public sealed record OrderPricingSnapshot(
    Guid OrderId,
    string CustomerId,
    string Currency,
    decimal GrandTotalAmount,
    string Status,
    IReadOnlyCollection<OrderPricingLineSnapshot> Lines,
    IReadOnlyCollection<PricingDiscountApplication> AppliedDiscounts);
