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
            order.Subtotal.Amount,
            order.Total.Amount,
            order.Status.ToString(),
            order.PlacedAtUtc,
            order.Lines
                .Select(line => new OrderLineDto(
                    line.ProductId,
                    line.Name,
                    line.UnitPrice.Currency,
                    line.UnitPrice.Amount,
                    line.Quantity))
                .ToList());
    }
}
