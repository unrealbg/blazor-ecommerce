using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;
using BuildingBlocks.Domain.Shared;
using Orders.Domain.Events;

namespace Orders.Domain.Orders;

public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> lines = [];

    private Order()
    {
    }

    private Order(
        Guid id,
        string customerId,
        string checkoutSessionId,
        IReadOnlyCollection<OrderLine> orderLines,
        OrderAddressSnapshot shippingAddress,
        OrderAddressSnapshot billingAddress,
        Money subtotal,
        Money total,
        DateTime placedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        CheckoutSessionId = checkoutSessionId;
        lines = [..orderLines];
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        Subtotal = subtotal;
        Total = total;
        PlacedAtUtc = placedAtUtc;
        Status = OrderStatus.PendingPayment;
        RowVersion = 0L;
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string CheckoutSessionId { get; private set; } = string.Empty;

    public IReadOnlyCollection<OrderLine> Lines => lines.AsReadOnly();

    public OrderAddressSnapshot ShippingAddress { get; private set; } = null!;

    public OrderAddressSnapshot BillingAddress { get; private set; } = null!;

    public Money Subtotal { get; private set; } = null!;

    public Money Total { get; private set; } = null!;

    public DateTime PlacedAtUtc { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public Guid? LastPaymentIntentId { get; private set; }

    public string? PaymentFailureMessage { get; private set; }

    public OrderStatus Status { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<Order> Create(
        string customerId,
        string checkoutSessionId,
        IReadOnlyCollection<OrderLineData> lineData,
        DateTime placedAtUtc,
        OrderAddressSnapshot? shippingAddress = null,
        OrderAddressSnapshot? billingAddress = null)
    {
        if (string.IsNullOrWhiteSpace(customerId))
        {
            return Result<Order>.Failure(new Error("orders.customer.required", "Customer id is required."));
        }

        if (string.IsNullOrWhiteSpace(checkoutSessionId))
        {
            return Result<Order>.Failure(new Error(
                "orders.checkout_session.required",
                "Checkout session id is required."));
        }

        if (lineData.Count == 0)
        {
            return Result<Order>.Failure(new Error("orders.lines.required", "Order must contain at least one line."));
        }

        var firstCurrency = lineData.First().UnitPrice.Currency;
        var subtotalAmount = 0m;
        var orderLines = new List<OrderLine>(lineData.Count);

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
            orderLines.Add(OrderLine.Create(line.ProductId, line.Name.Trim(), line.UnitPrice, line.Quantity));
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
            checkoutSessionId.Trim(),
            orderLines,
            shippingAddress ?? OrderAddressSnapshot.Empty(),
            billingAddress ?? OrderAddressSnapshot.Empty(),
            subtotalResult.Value,
            totalResult.Value,
            placedAtUtc));
    }

    public Result MarkPaid(Guid paymentIntentId, DateTime paidAtUtc)
    {
        if (paymentIntentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "orders.payment_intent.required",
                "Payment intent id is required."));
        }

        if (Status is not (OrderStatus.PendingPayment or OrderStatus.PaymentFailed))
        {
            return Result.Failure(new Error(
                "orders.status.transition.invalid",
                $"Cannot move order from '{Status}' to 'Paid'."));
        }

        var wasPaid = Status == OrderStatus.Paid;

        Status = OrderStatus.Paid;
        LastPaymentIntentId = paymentIntentId;
        PaymentFailureMessage = null;
        PaidAtUtc ??= paidAtUtc;
        Touch();

        if (!wasPaid)
        {
            RaiseDomainEvent(new OrderPlaced(Id, CustomerId, Total.Currency, Total.Amount));
        }

        return Result.Success();
    }

    public Result MarkPaymentFailed(Guid paymentIntentId, string? failureMessage)
    {
        if (Status is not (OrderStatus.PendingPayment or OrderStatus.PaymentFailed))
        {
            return Result.Failure(new Error(
                "orders.status.transition.invalid",
                $"Cannot move order from '{Status}' to 'PaymentFailed'."));
        }

        Status = OrderStatus.PaymentFailed;
        LastPaymentIntentId = paymentIntentId == Guid.Empty ? LastPaymentIntentId : paymentIntentId;
        PaymentFailureMessage = string.IsNullOrWhiteSpace(failureMessage) ? null : failureMessage.Trim();
        Touch();
        return Result.Success();
    }

    public Result MarkCancelled(Guid paymentIntentId, string? reason)
    {
        if (Status is not (OrderStatus.PendingPayment or OrderStatus.PaymentFailed))
        {
            return Result.Failure(new Error(
                "orders.status.transition.invalid",
                $"Cannot move order from '{Status}' to 'Cancelled'."));
        }

        Status = OrderStatus.Cancelled;
        LastPaymentIntentId = paymentIntentId == Guid.Empty ? LastPaymentIntentId : paymentIntentId;
        PaymentFailureMessage = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        Touch();
        return Result.Success();
    }

    public Result MarkRefunded(Guid paymentIntentId, bool partial)
    {
        if (Status is not (OrderStatus.Paid or OrderStatus.PartiallyRefunded))
        {
            return Result.Failure(new Error(
                "orders.status.transition.invalid",
                $"Cannot move order from '{Status}' to refund state."));
        }

        Status = partial ? OrderStatus.PartiallyRefunded : OrderStatus.Refunded;
        LastPaymentIntentId = paymentIntentId == Guid.Empty ? LastPaymentIntentId : paymentIntentId;
        Touch();
        return Result.Success();
    }

    private void Touch()
    {
        RowVersion++;
    }
}
