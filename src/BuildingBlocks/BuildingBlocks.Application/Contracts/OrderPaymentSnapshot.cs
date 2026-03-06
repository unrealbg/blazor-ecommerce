namespace BuildingBlocks.Application.Contracts;

public sealed record OrderPaymentSnapshot(
    Guid OrderId,
    string CustomerId,
    string CheckoutSessionId,
    decimal TotalAmount,
    string Currency,
    string Status,
    IReadOnlyCollection<OrderPaymentLineSnapshot> Lines);
