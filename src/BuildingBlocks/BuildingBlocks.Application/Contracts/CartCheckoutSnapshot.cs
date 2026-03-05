namespace BuildingBlocks.Application.Contracts;

public sealed record CartCheckoutSnapshot(
    Guid CartId,
    string CustomerId,
    IReadOnlyCollection<CartCheckoutLineSnapshot> Lines);
