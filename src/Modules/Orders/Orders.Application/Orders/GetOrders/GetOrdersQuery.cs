using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.GetOrders;

public sealed record GetOrdersQuery : IQuery<IReadOnlyCollection<OrderDto>>;
