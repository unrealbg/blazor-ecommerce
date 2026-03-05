using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.AddItem;

public sealed record AddItemToCartCommand(
    string CustomerId,
    Guid ProductId,
    int Quantity) : ICommand<Guid>;
