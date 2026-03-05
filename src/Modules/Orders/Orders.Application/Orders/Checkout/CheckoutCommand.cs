using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.Checkout;

public sealed record CheckoutCommand(string CustomerId) : ICommand<Guid>;
