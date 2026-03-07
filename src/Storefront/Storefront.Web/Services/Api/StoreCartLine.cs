namespace Storefront.Web.Services.Api;

public sealed class StoreCartLine
{
    public StoreCartLine()
    {
    }

    public StoreCartLine(
        Guid productId,
        string productName,
        string currency,
        decimal amount,
        int quantity)
    {
        ProductId = productId;
        VariantId = productId;
        ProductName = productName;
        Currency = currency;
        BaseUnitAmount = amount;
        FinalUnitAmount = amount;
        LineTotalAmount = amount * quantity;
        Quantity = quantity;
        AppliedDiscounts = [];
    }

    public Guid ProductId { get; init; }

    public Guid VariantId { get; init; }

    public string? Sku { get; init; }

    public string ProductName { get; init; } = string.Empty;

    public string? VariantName { get; init; }

    public string? SelectedOptionsJson { get; init; }

    public string? ImageUrl { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal BaseUnitAmount { get; init; }

    public decimal? CompareAtUnitAmount { get; init; }

    public decimal FinalUnitAmount { get; init; }

    public decimal LineTotalAmount { get; init; }

    public decimal DiscountTotalAmount { get; init; }

    public int Quantity { get; init; }

    public IReadOnlyCollection<StorePricingDiscountApplication> AppliedDiscounts { get; init; } = [];
}
