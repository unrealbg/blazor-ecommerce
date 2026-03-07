namespace BuildingBlocks.Application.Contracts;

public sealed record ProductVariantSnapshot(
    Guid Id,
    Guid ProductId,
    string Sku,
    string? Name,
    string? Slug,
    string? Barcode,
    string Currency,
    decimal Amount,
    decimal? CompareAtAmount,
    decimal? WeightKg,
    bool IsActive,
    int Position,
    bool IsTracked,
    bool AllowBackorder,
    int? AvailableQuantity,
    bool IsInStock,
    string? ImageUrl,
    IReadOnlyCollection<ProductOptionSelectionSnapshot> SelectedOptions);
