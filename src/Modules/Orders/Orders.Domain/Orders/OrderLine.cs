using BuildingBlocks.Domain.Shared;

namespace Orders.Domain.Orders;

public sealed class OrderLine
{
    private OrderLine()
    {
    }

    private OrderLine(Guid productId, string name, Money unitPrice, int quantity)
    {
        ProductId = productId;
        Name = name;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Money UnitPrice { get; private set; } = null!;

    public int Quantity { get; private set; }

    public static OrderLine Create(Guid productId, string name, Money unitPrice, int quantity)
    {
        return new OrderLine(productId, name, unitPrice, quantity);
    }
}
