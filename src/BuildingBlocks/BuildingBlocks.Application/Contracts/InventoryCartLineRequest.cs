namespace BuildingBlocks.Application.Contracts;

public sealed record InventoryCartLineRequest(
    Guid ProductId,
    string? Sku,
    int Quantity);
