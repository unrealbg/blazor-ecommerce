using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.GetCart;

public sealed record GetCartQuery(string CustomerId) : IQuery<CartDto?>;
