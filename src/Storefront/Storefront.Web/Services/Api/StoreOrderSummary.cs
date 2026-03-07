namespace Storefront.Web.Services.Api;

public sealed class StoreOrderSummary
{
    public StoreOrderSummary()
    {
    }

    public StoreOrderSummary(
        Guid id,
        string customerId,
        string currency,
        decimal subtotalAmount,
        decimal shippingPriceAmount,
        string shippingCurrency,
        string shippingMethodCode,
        string shippingMethodName,
        decimal totalAmount,
        string status,
        string fulfillmentStatus,
        DateTime placedAtUtc,
        StoreOrderAddress shippingAddress,
        StoreOrderAddress billingAddress,
        IReadOnlyCollection<StoreOrderLine> lines)
    {
        Id = id;
        CustomerId = customerId;
        Currency = currency;
        SubtotalAmount = subtotalAmount;
        ShippingPriceAmount = shippingPriceAmount;
        ShippingCurrency = shippingCurrency;
        ShippingMethodCode = shippingMethodCode;
        ShippingMethodName = shippingMethodName;
        TotalAmount = totalAmount;
        Status = status;
        FulfillmentStatus = fulfillmentStatus;
        PlacedAtUtc = placedAtUtc;
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        Lines = lines;
        SubtotalBeforeDiscountAmount = subtotalAmount;
    }

    public Guid Id { get; init; }

    public string CustomerId { get; init; } = string.Empty;

    public string Currency { get; init; } = "EUR";

    public decimal SubtotalAmount { get; init; }

    public decimal ShippingPriceAmount { get; init; }

    public string ShippingCurrency { get; init; } = "EUR";

    public string ShippingMethodCode { get; init; } = string.Empty;

    public string ShippingMethodName { get; init; } = string.Empty;

    public decimal TotalAmount { get; init; }

    public string Status { get; init; } = string.Empty;

    public string FulfillmentStatus { get; init; } = string.Empty;

    public DateTime PlacedAtUtc { get; init; }

    public StoreOrderAddress ShippingAddress { get; init; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null);

    public StoreOrderAddress BillingAddress { get; init; } = new(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null);

    public IReadOnlyCollection<StoreOrderLine> Lines { get; init; } = [];

    public decimal SubtotalBeforeDiscountAmount { get; init; }

    public decimal LineDiscountTotalAmount { get; init; }

    public decimal CartDiscountTotalAmount { get; init; }

    public decimal ShippingDiscountTotalAmount { get; init; }

    public string? AppliedCouponsJson { get; init; }

    public string? AppliedPromotionsJson { get; init; }
}
