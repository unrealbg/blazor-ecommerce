using BuildingBlocks.Domain.Shared;

namespace Cart.Domain.Carts;

public sealed class CartLine
{
    private CartLine()
    {
    }

    private CartLine(Guid productId, string productName, Money unitPrice, int quantity)
    {
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = string.Empty;

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public static CartLine Create(Guid productId, string productName, Money unitPrice, int quantity)
    {
        return new CartLine(productId, productName, unitPrice, quantity);
    }

    internal void IncreaseQuantity(int quantity)
    {
        Quantity += quantity;
    }
}
