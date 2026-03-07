namespace BuildingBlocks.Application.Contracts;

public sealed record CartCheckoutLineSnapshot(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string? VariantName,
    string? Sku,
    string? ImageUrl,
    string? SelectedOptionsJson,
    string Currency,
    decimal UnitAmount,
    int Quantity);
