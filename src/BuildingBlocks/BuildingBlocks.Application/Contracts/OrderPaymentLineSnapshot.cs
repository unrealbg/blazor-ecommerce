namespace BuildingBlocks.Application.Contracts;

public sealed record OrderPaymentLineSnapshot(
    Guid ProductId,
    string? Sku,
    int Quantity);
