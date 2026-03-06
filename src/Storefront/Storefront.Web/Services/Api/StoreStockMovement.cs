namespace Storefront.Web.Services.Api;

public sealed record StoreStockMovement(
    Guid Id,
    Guid ProductId,
    string? Sku,
    string Type,
    int QuantityDelta,
    Guid? ReferenceId,
    string? Reason,
    DateTime CreatedAtUtc,
    string? CreatedBy);
