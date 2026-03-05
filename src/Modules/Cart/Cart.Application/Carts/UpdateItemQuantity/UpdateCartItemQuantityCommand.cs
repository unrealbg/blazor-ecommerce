using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.UpdateItemQuantity;

public sealed record UpdateCartItemQuantityCommand(
    string CustomerId,
    Guid ProductId,
    int Quantity) : ICommand<Guid>;
