using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Domain.Events;

namespace Orders.Domain.Orders;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        string customerId,
        IReadOnlyCollection<OrderLine> lines,
        OrderAddressSnapshot shippingAddress,
        OrderAddressSnapshot billingAddress,
        Money subtotal,
        Money total,
        DateTime placedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        _lines = [..lines];
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        Subtotal = subtotal;
        Total = total;
        PlacedAtUtc = placedAtUtc;
        Status = OrderStatus.Placed;
        RowVersion = 0L;

        RaiseDomainEvent(new OrderPlaced(Id, CustomerId, Total.Currency, Total.Amount));
    }

    public string CustomerId { get; private set; } = string.Empty;

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public OrderAddressSnapshot ShippingAddress { get; private set; } = null!;

    public OrderAddressSnapshot BillingAddress { get; private set; } = null!;

    public Money Subtotal { get; private set; } = null!;

    public Money Total { get; private set; } = null!;

    public DateTime PlacedAtUtc { get; private set; }

    public OrderStatus Status { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<Order> Create(
        string customerId,
        IReadOnlyCollection<OrderLineData> lineData,
        DateTime placedAtUtc,
        OrderAddressSnapshot? shippingAddress = null,
        OrderAddressSnapshot? billingAddress = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result<Order>.Failure(new Error("orders.customer.required", "Customer id is required."));
        }

        if (lineData.Count == 0)
        {
            return Result<Order>.Failure(new Error("orders.lines.required", "Order must contain at least one line."));
        }

        var firstCurrency = lineData.First().UnitPrice.Currency;
        var subtotalAmount = 0m;
        var lines = new List<OrderLine>(lineData.Count);

        foreach (var line in lineData)
        {
            if (!string.Equals(firstCurrency, line.UnitPrice.Currency, StringComparison.Ordinal))
            {
                return Result<Order>.Failure(
                    new Error("orders.currency.mismatch", "All order lines must use the same currency."));
            }

            if (line.Quantity <= 0)
            {
                return Result<Order>.Failure(
                    new Error("orders.line.quantity.invalid", "Order line quantity must be greater than zero."));
            }

            if (string.IsNullOrWhiteSpace(line.Name))
            {
                return Result<Order>.Failure(
                    new Error("orders.line.name.required", "Order line name is required."));
            }

            subtotalAmount += Money.Round(line.UnitPrice.Amount * line.Quantity);
            lines.Add(OrderLine.Create(line.ProductId, line.Name.Trim(), line.UnitPrice, line.Quantity));
        }

        var subtotalResult = Money.Create(firstCurrency, subtotalAmount);
        if (subtotalResult.IsFailure)
        {
            return Result<Order>.Failure(subtotalResult.Error);
        }

        var totalResult = Money.Create(firstCurrency, subtotalAmount);
        if (totalResult.IsFailure)
        {
            return Result<Order>.Failure(totalResult.Error);
        }

        return Result<Order>.Success(new Order(
            Guid.NewGuid(),
            customerId.Trim(),
            lines,
            shippingAddress ?? OrderAddressSnapshot.Empty(),
            billingAddress ?? OrderAddressSnapshot.Empty(),
            subtotalResult.Value,
            totalResult.Value,
            placedAtUtc));
    }
}
