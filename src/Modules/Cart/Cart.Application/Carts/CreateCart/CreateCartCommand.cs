using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.CreateCart;

public sealed record CreateCartCommand(Guid CustomerId) : ICommand<Guid>;
