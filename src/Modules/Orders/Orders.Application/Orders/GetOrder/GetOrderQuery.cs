using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.GetOrder;

public sealed record GetOrderQuery(Guid OrderId) : IQuery<OrderDto?>;
