namespace BuildingBlocks.Application.Contracts;

public sealed record OrderPaymentLineSnapshot(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    int Quantity);
