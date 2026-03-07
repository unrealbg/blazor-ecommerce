using BuildingBlocks.Domain.Shared;

namespace Cart.Domain.Carts;

public sealed class CartLine
{
    private CartLine()
    {
    }

    private CartLine(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        string? imageUrl,
        Money unitPrice,
        int quantity)
    {
        ProductId = productId;
        VariantId = variantId;
        Sku = sku;
        ProductName = productName;
        VariantName = variantName;
        SelectedOptionsJson = selectedOptionsJson;
        ImageUrl = imageUrl;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public Guid VariantId { get; private set; }

    public string? Sku { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public string? VariantName { get; private set; }

    public string? SelectedOptionsJson { get; private set; }

    public string? ImageUrl { get; private set; }

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public static CartLine Create(
        Guid productId,
        Guid variantId,
        string? sku,
        string productName,
        string? variantName,
        string? selectedOptionsJson,
        string? imageUrl,
        Money unitPrice,
        int quantity)
    {
        return new CartLine(
            productId,
            variantId,
            sku,
            productName,
            variantName,
            selectedOptionsJson,
            imageUrl,
            unitPrice,
            quantity);
    }

    internal void IncreaseQuantity(int quantity)
    {
        Quantity += quantity;
    }

    internal void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }
}
