using BuildingBlocks.Domain.Shared;

namespace Orders.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    private OrderLine(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        decimal baseUnitAmount,
        Money unitPrice,
        decimal? compareAtPriceAmount,
        decimal discountTotalAmount,
        string? appliedDiscountsJson,
        int quantity)
    {
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        ProductName = productName;
        VariantName = variantName;
        SelectedOptionsJson = selectedOptionsJson;
        BaseUnitAmount = baseUnitAmount;
        UnitPrice = unitPrice;
        CompareAtPriceAmount = compareAtPriceAmount;
        DiscountTotalAmount = discountTotalAmount;
        AppliedDiscountsJson = appliedDiscountsJson;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public Guid VariantId { get; private set; }

    public string? Sku { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string? VariantName { get; private set; }

    public string? SelectedOptionsJson { get; private set; }

    public decimal BaseUnitAmount { get; private set; }

    public Money UnitPrice { get; private set; } = null!;

    public decimal? CompareAtPriceAmount { get; private set; }

    public decimal DiscountTotalAmount { get; private set; }

    public string? AppliedDiscountsJson { get; private set; }

    public int Quantity { get; private set; }

    public static OrderLine Create(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        decimal baseUnitAmount,
        Money unitPrice,
        decimal? compareAtPriceAmount,
        decimal discountTotalAmount,
        string? appliedDiscountsJson,
        int quantity)
    {
        return new OrderLine(
            productId,
            variantId,
            sku,
            productName,
            variantName,
            selectedOptionsJson,
            baseUnitAmount,
            unitPrice,
            compareAtPriceAmount,
            discountTotalAmount,
            appliedDiscountsJson,
            quantity);
    }
}
