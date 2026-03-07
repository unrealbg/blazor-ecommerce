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
        Money unitPrice,
        int quantity)
    {
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        ProductName = productName;
        VariantName = variantName;
        SelectedOptionsJson = selectedOptionsJson;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public Guid VariantId { get; private set; }

    public string? Sku { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string? VariantName { get; private set; }

    public string? SelectedOptionsJson { get; private set; }

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public static OrderLine Create(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        Money unitPrice,
        int quantity)
    {
        return new OrderLine(
            productId,
            variantId,
            sku,
            productName,
            variantName,
            selectedOptionsJson,
            unitPrice,
            quantity);
    }
}
