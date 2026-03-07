namespace BuildingBlocks.Application.Contracts;

public sealed record InventoryCartLineRequest(
    Guid ProductId,
    Guid VariantId,
    string? Sku,
    int Quantity);
