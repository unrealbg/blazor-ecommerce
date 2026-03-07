namespace Storefront.Web.Services.Api;

public sealed class StoreProductVariant
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string? Slug { get; init; }

    public string? Barcode { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal Amount { get; init; }

    public decimal? CompareAtAmount { get; init; }

    public decimal? WeightKg { get; init; }

    public bool IsActive { get; init; }

    public int Position { get; init; }

    public bool IsTracked { get; init; }

    public bool AllowBackorder { get; init; }

    public int? AvailableQuantity { get; init; }

    public bool IsInStock { get; init; }

    public string? ImageUrl { get; init; }

    public IReadOnlyCollection<StoreProductOptionSelection> SelectedOptions { get; init; } = [];
}
