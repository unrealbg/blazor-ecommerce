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
        string shippingMethodCode,
        string shippingMethodName,
        Money subtotalBeforeDiscount,
        Money shippingPrice,
        Money lineDiscountTotal,
        Money cartDiscountTotal,
        Money shippingDiscountTotal,
        Money subtotal,
        Money total,
        string? appliedCouponsJson,
        string? appliedPromotionsJson,
        DateTime placedAtUtc)
    {
        Id = id;
        CustomerId = customerId;
        CheckoutSessionId = checkoutSessionId;
        lines = [..orderLines];
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        ShippingMethodCode = shippingMethodCode;
        ShippingMethodName = shippingMethodName;
        SubtotalBeforeDiscount = subtotalBeforeDiscount;
        ShippingPrice = shippingPrice;
        LineDiscountTotal = lineDiscountTotal;
        CartDiscountTotal = cartDiscountTotal;
        ShippingDiscountTotal = shippingDiscountTotal;
        Subtotal = subtotal;
        Total = total;
        AppliedCouponsJson = appliedCouponsJson;
        AppliedPromotionsJson = appliedPromotionsJson;
        PlacedAtUtc = placedAtUtc;
        Status = OrderStatus.PendingPayment;
        FulfillmentStatus = OrderFulfillmentStatus.Unfulfilled;
        RowVersion = 0L;
    }

    public string CustomerId { get; private set; } = string.Empty;

    public string CheckoutSessionId { get; private set; } = string.Empty;

    public IReadOnlyCollection<OrderLine> Lines => lines.AsReadOnly();

    public OrderAddressSnapshot ShippingAddress { get; private set; } = null!;

    public OrderAddressSnapshot BillingAddress { get; private set; } = null!;

    public string ShippingMethodCode { get; private set; } = string.Empty;

    public string ShippingMethodName { get; private set; } = string.Empty;

    public Money SubtotalBeforeDiscount { get; private set; } = null!;

    public Money ShippingPrice { get; private set; } = null!;

    public Money LineDiscountTotal { get; private set; } = null!;

    public Money CartDiscountTotal { get; private set; } = null!;

    public Money ShippingDiscountTotal { get; private set; } = null!;

    public Money Subtotal { get; private set; } = null!;

    public Money Total { get; private set; } = null!;

    public string? AppliedCouponsJson { get; private set; }

    public string? AppliedPromotionsJson { get; private set; }

    public DateTime PlacedAtUtc { get; private set; }

    public DateTime? PaidAtUtc { get; private set; }

    public Guid? LastPaymentIntentId { get; private set; }

    public Guid? LastShipmentId { get; private set; }

    public string? PaymentFailureMessage { get; private set; }

    public OrderStatus Status { get; private set; }

    public OrderFulfillmentStatus FulfillmentStatus { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<Order> Create(
        string customerId,
        string checkoutSessionId,
        IReadOnlyCollection<OrderLineData> lineData,
        DateTime placedAtUtc,
        OrderAddressSnapshot? shippingAddress = null,
        OrderAddressSnapshot? billingAddress = null,
        string? shippingMethodCode = null,
        string? shippingMethodName = null,
        decimal shippingPriceAmount = 0m,
        string? shippingCurrency = null,
        decimal? subtotalBeforeDiscountAmount = null,
        decimal lineDiscountTotalAmount = 0m,
        decimal cartDiscountTotalAmount = 0m,
        decimal shippingDiscountTotalAmount = 0m,
        string? appliedCouponsJson = null,
        string? appliedPromotionsJson = null)
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

        if (shippingPriceAmount < 0m)
        {
            return Result<Order>.Failure(new Error(
                "orders.shipping.price.invalid",
                "Shipping price cannot be negative."));
        }

        var firstCurrency = lineData.First().UnitPrice.Currency;
        var finalLineSubtotalAmount = 0m;
        var baseSubtotalAmount = 0m;
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

            if (line.VariantId == Guid.Empty)
            {
                return Result<Order>.Failure(
                    new Error("orders.line.variant.required", "Order line variant id is required."));
            }

            if (string.IsNullOrWhiteSpace(line.ProductName))
            {
                return Result<Order>.Failure(
                    new Error("orders.line.name.required", "Order line product name is required."));
            }

            var baseUnitAmount = line.BaseUnitAmount ?? line.UnitPrice.Amount;
            if (baseUnitAmount < 0m)
            {
                return Result<Order>.Failure(
                    new Error("orders.line.base_price.invalid", "Order line base unit amount cannot be negative."));
            }

            if (line.DiscountTotalAmount < 0m)
            {
                return Result<Order>.Failure(
                    new Error("orders.line.discount.invalid", "Order line discount cannot be negative."));
            }

            baseSubtotalAmount += Money.Round(baseUnitAmount * line.Quantity);
            finalLineSubtotalAmount += Money.Round(line.UnitPrice.Amount * line.Quantity);
            orderLines.Add(OrderLine.Create(
                line.ProductId,
                line.VariantId,
                line.Sku,
                line.ProductName.Trim(),
                string.IsNullOrWhiteSpace(line.VariantName) ? null : line.VariantName.Trim(),
                string.IsNullOrWhiteSpace(line.SelectedOptionsJson) ? null : line.SelectedOptionsJson.Trim(),
                baseUnitAmount,
                line.UnitPrice,
                line.CompareAtPriceAmount,
                line.DiscountTotalAmount,
                string.IsNullOrWhiteSpace(line.AppliedDiscountsJson) ? null : line.AppliedDiscountsJson.Trim(),
                line.Quantity));
        }

        var resolvedSubtotalBeforeDiscount = subtotalBeforeDiscountAmount ?? baseSubtotalAmount;
        var subtotalBeforeDiscountResult = Money.Create(firstCurrency, resolvedSubtotalBeforeDiscount);
        if (subtotalBeforeDiscountResult.IsFailure)
        {
            return Result<Order>.Failure(subtotalBeforeDiscountResult.Error);
        }

        var lineDiscountTotalResult = Money.Create(firstCurrency, lineDiscountTotalAmount);
        if (lineDiscountTotalResult.IsFailure)
        {
            return Result<Order>.Failure(lineDiscountTotalResult.Error);
        }

        var cartDiscountTotalResult = Money.Create(firstCurrency, cartDiscountTotalAmount);
        if (cartDiscountTotalResult.IsFailure)
        {
            return Result<Order>.Failure(cartDiscountTotalResult.Error);
        }

        var subtotalAmount = Money.Round(finalLineSubtotalAmount);
        if (subtotalAmount < 0m)
        {
            return Result<Order>.Failure(new Error(
                "orders.subtotal.invalid",
                "Order subtotal cannot be negative."));
        }

        var subtotalResult = Money.Create(firstCurrency, subtotalAmount);
        if (subtotalResult.IsFailure)
        {
            return Result<Order>.Failure(subtotalResult.Error);
        }

        var resolvedShippingCurrency = string.IsNullOrWhiteSpace(shippingCurrency)
            ? firstCurrency
            : shippingCurrency.Trim().ToUpperInvariant();

        if (!string.Equals(firstCurrency, resolvedShippingCurrency, StringComparison.Ordinal))
        {
            return Result<Order>.Failure(new Error(
                "orders.shipping.currency.mismatch",
                "Shipping currency must match order currency."));
        }

        var shippingPriceResult = Money.Create(resolvedShippingCurrency, shippingPriceAmount);
        if (shippingPriceResult.IsFailure)
        {
            return Result<Order>.Failure(shippingPriceResult.Error);
        }

        var shippingDiscountTotalResult = Money.Create(firstCurrency, shippingDiscountTotalAmount);
        if (shippingDiscountTotalResult.IsFailure)
        {
            return Result<Order>.Failure(shippingDiscountTotalResult.Error);
        }

        var finalShippingAmount = Money.Round(shippingPriceAmount - shippingDiscountTotalAmount);
        if (finalShippingAmount < 0m)
        {
            return Result<Order>.Failure(new Error(
                "orders.shipping.total.invalid",
                "Final shipping amount cannot be negative."));
        }

        var finalShippingPriceResult = Money.Create(resolvedShippingCurrency, finalShippingAmount);
        if (finalShippingPriceResult.IsFailure)
        {
            return Result<Order>.Failure(finalShippingPriceResult.Error);
        }

        var totalAmount = Money.Round(subtotalAmount + finalShippingAmount);
        var totalResult = Money.Create(firstCurrency, totalAmount);
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
            string.IsNullOrWhiteSpace(shippingMethodCode) ? "standard" : shippingMethodCode.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(shippingMethodName) ? "Standard Delivery" : shippingMethodName.Trim(),
            subtotalBeforeDiscountResult.Value,
            finalShippingPriceResult.Value,
            lineDiscountTotalResult.Value,
            cartDiscountTotalResult.Value,
            shippingDiscountTotalResult.Value,
            subtotalResult.Value,
            totalResult.Value,
            string.IsNullOrWhiteSpace(appliedCouponsJson) ? null : appliedCouponsJson.Trim(),
            string.IsNullOrWhiteSpace(appliedPromotionsJson) ? null : appliedPromotionsJson.Trim(),
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

    public Result MarkFulfillmentPending(Guid shipmentId)
    {
        if (shipmentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "orders.shipment.required",
                "Shipment id is required."));
        }

        if (Status is not (OrderStatus.Paid or OrderStatus.PartiallyRefunded or OrderStatus.Refunded))
        {
            return Result.Failure(new Error(
                "orders.fulfillment.not_allowed",
                "Order is not eligible for fulfillment."));
        }

        if (FulfillmentStatus == OrderFulfillmentStatus.Fulfilled)
        {
            return Result.Failure(new Error(
                "orders.fulfillment.completed",
                "Order is already fulfilled."));
        }

        LastShipmentId = shipmentId;
        FulfillmentStatus = OrderFulfillmentStatus.FulfillmentPending;
        Touch();
        return Result.Success();
    }

    public Result MarkFulfilled(Guid shipmentId)
    {
        if (shipmentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "orders.shipment.required",
                "Shipment id is required."));
        }

        if (Status is not (OrderStatus.Paid or OrderStatus.PartiallyRefunded or OrderStatus.Refunded))
        {
            return Result.Failure(new Error(
                "orders.fulfillment.not_allowed",
                "Order is not eligible for fulfillment."));
        }

        if (FulfillmentStatus == OrderFulfillmentStatus.Returned)
        {
            return Result.Failure(new Error(
                "orders.fulfillment.returned",
                "Returned order cannot be fulfilled."));
        }

        LastShipmentId = shipmentId;
        FulfillmentStatus = OrderFulfillmentStatus.Fulfilled;
        Touch();
        return Result.Success();
    }

    public Result MarkReturned(Guid shipmentId)
    {
        if (shipmentId == Guid.Empty)
        {
            return Result.Failure(new Error(
                "orders.shipment.required",
                "Shipment id is required."));
        }

        if (FulfillmentStatus is not (OrderFulfillmentStatus.Fulfilled or OrderFulfillmentStatus.FulfillmentPending))
        {
            return Result.Failure(new Error(
                "orders.fulfillment.return.not_allowed",
                "Order cannot be marked as returned."));
        }

        LastShipmentId = shipmentId;
        FulfillmentStatus = OrderFulfillmentStatus.Returned;
        Touch();
        return Result.Success();
    }

    private void Touch()
    {
        RowVersion++;
    }
}
