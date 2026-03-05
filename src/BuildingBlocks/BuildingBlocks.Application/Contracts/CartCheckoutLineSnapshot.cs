namespace BuildingBlocks.Application.Contracts;

public sealed record CartCheckoutLineSnapshot(
    Guid ProductId,
    string Name,
    string Currency,
    decimal UnitAmount,
    int Quantity);
