using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.AddItem;

public sealed record AddItemToCartCommand(
    string CustomerId,
    Guid ProductId,
    Guid VariantId,
    int Quantity) : ICommand<Guid>;
