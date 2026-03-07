using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.GetOrder;

public sealed class GetOrderQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return null;
        }

        return new OrderDto(
            order.Id,
            order.CustomerId,
            order.Total.Currency,
            order.SubtotalBeforeDiscount.Amount,
            order.Subtotal.Amount,
            order.LineDiscountTotal.Amount,
            order.CartDiscountTotal.Amount,
            order.ShippingPrice.Amount,
            order.ShippingDiscountTotal.Amount,
            order.ShippingPrice.Currency,
            order.ShippingMethodCode,
            order.ShippingMethodName,
            order.Total.Amount,
            order.AppliedCouponsJson,
            order.AppliedPromotionsJson,
            order.Status.ToString(),
            order.FulfillmentStatus.ToString(),
            order.PlacedAtUtc,
            new OrderAddressDto(
                order.ShippingAddress.FirstName,
                order.ShippingAddress.LastName,
                order.ShippingAddress.Street,
                order.ShippingAddress.City,
                order.ShippingAddress.PostalCode,
                order.ShippingAddress.Country,
                order.ShippingAddress.Phone),
            new OrderAddressDto(
                order.BillingAddress.FirstName,
                order.BillingAddress.LastName,
                order.BillingAddress.Street,
                order.BillingAddress.City,
                order.BillingAddress.PostalCode,
                order.BillingAddress.Country,
                order.BillingAddress.Phone),
            order.Lines
                .Select(line => new OrderLineDto(
                    line.ProductId,
                    line.VariantId,
                    line.Sku,
                    string.IsNullOrWhiteSpace(line.VariantName)
                        ? line.ProductName
                        : $"{line.ProductName} ({line.VariantName})",
                    line.VariantName,
                    line.SelectedOptionsJson,
                    line.UnitPrice.Currency,
                    line.BaseUnitAmount,
                    line.UnitPrice.Amount,
                    line.CompareAtPriceAmount,
                    line.DiscountTotalAmount,
                    line.AppliedDiscountsJson,
                    line.Quantity))
                .ToList());
    }
}
