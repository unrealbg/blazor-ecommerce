using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.GetOrders;

public sealed class GetOrdersQueryHandler(IOrderRepository orderRepository)
    : IQueryHandler<GetOrdersQuery, IReadOnlyCollection<OrderDto>>
{
    public async Task<IReadOnlyCollection<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await orderRepository.ListAsync(cancellationToken);

        return orders
            .Select(order => new OrderDto(
                order.Id,
                order.CartId,
                order.CustomerId,
                order.Total.Currency,
                order.Total.Amount,
                order.Status.ToString(),
                order.CreatedOnUtc))
            .ToList();
    }
}
