using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.GetCartById;

public sealed record GetCartByIdQuery(Guid CartId) : IQuery<CartDto?>;
