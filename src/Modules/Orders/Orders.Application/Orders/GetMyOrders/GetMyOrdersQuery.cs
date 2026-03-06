using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.GetMyOrders;

public sealed record GetMyOrdersQuery(Guid UserId) : IQuery<IReadOnlyCollection<OrderDto>>;
