namespace BuildingBlocks.Application.Contracts;

public sealed record CartCheckoutSnapshot(
    Guid CartId,
    string CustomerId,
    string? AppliedCouponCode,
    IReadOnlyCollection<CartCheckoutLineSnapshot> Lines);
