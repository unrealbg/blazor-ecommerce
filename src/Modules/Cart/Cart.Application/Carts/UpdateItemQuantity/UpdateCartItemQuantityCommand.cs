using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.UpdateItemQuantity;

public sealed record UpdateCartItemQuantityCommand(
    string CustomerId,
    Guid VariantId,
    int Quantity) : ICommand<Guid>;
