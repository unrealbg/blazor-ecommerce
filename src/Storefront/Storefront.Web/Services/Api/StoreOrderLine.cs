namespace Storefront.Web.Services.Api;

public sealed class StoreOrderLine
{
    public StoreOrderLine()
    {
    }

    public StoreOrderLine(
        Guid productId,
        string name,
        string currency,
        decimal unitAmount,
        int quantity)
    {
        ProductId = productId;
        VariantId = productId;
        Name = name;
        Currency = currency;
        BaseUnitAmount = unitAmount;
        FinalUnitAmount = unitAmount;
        Quantity = quantity;
    }

    public Guid ProductId { get; init; }

    public Guid VariantId { get; init; }

    public string? Sku { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? VariantName { get; init; }

    public string? SelectedOptionsJson { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal BaseUnitAmount { get; init; }

    public decimal FinalUnitAmount { get; init; }

    public decimal? CompareAtPriceAmount { get; init; }

    public decimal DiscountTotalAmount { get; init; }

    public string? AppliedDiscountsJson { get; init; }

    public int Quantity { get; init; }
}
