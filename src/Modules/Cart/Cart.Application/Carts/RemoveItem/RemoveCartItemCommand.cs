using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.RemoveItem;

public sealed record RemoveCartItemCommand(
    string CustomerId,
    Guid VariantId) : ICommand<Guid>;
