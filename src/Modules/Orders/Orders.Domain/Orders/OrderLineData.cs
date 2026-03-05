using BuildingBlocks.Domain.Shared;

namespace Orders.Domain.Orders;

public sealed record OrderLineData(Guid ProductId, string Name, Money UnitPrice, int Quantity);
