namespace Storefront.Web.Services.Api;

public sealed class StoreCart
{
    public StoreCart()
    {
    }

    public StoreCart(
        Guid id,
        string customerId,
        IReadOnlyCollection<StoreCartLine> lines,
        IReadOnlyCollection<string> messages)
    {
        Id = id;
        CustomerId = customerId;
        Lines = lines;
        Messages = messages;
        Currency = lines.FirstOrDefault()?.Currency ?? "EUR";
        SubtotalBeforeDiscountAmount = lines.Sum(line => line.BaseUnitAmount * line.Quantity);
        SubtotalAmount = lines.Sum(line => line.LineTotalAmount);
        LineDiscountTotalAmount = lines.Sum(line => line.DiscountTotalAmount);
        CartDiscountTotalAmount = 0m;
        GrandTotalAmount = SubtotalAmount;
        AppliedDiscounts = [];
    }

    public Guid Id { get; init; }

    public string CustomerId { get; init; } = string.Empty;

    public string? AppliedCouponCode { get; init; }

    public string Currency { get; init; } = "EUR";

    public decimal SubtotalBeforeDiscountAmount { get; init; }

    public decimal SubtotalAmount { get; init; }

    public decimal LineDiscountTotalAmount { get; init; }

    public decimal CartDiscountTotalAmount { get; init; }

    public decimal GrandTotalAmount { get; init; }

    public IReadOnlyCollection<StoreCartLine> Lines { get; init; } = [];

    public IReadOnlyCollection<StorePricingDiscountApplication> AppliedDiscounts { get; init; } = [];

    public IReadOnlyCollection<string> Messages { get; init; } = [];
}
