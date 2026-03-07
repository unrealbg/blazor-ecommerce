namespace Backoffice.Application.Backoffice;

public sealed record BackofficeOrderLineDto(
    Guid ProductId,
    Guid VariantId,
    string ProductName,
    string? VariantName,
    string? Sku,
    string Currency,
    decimal UnitPriceAmount,
    decimal LineDiscountAmount,
    decimal? CompareAtAmount,
    int Quantity,
    string? SelectedOptionsJson);
