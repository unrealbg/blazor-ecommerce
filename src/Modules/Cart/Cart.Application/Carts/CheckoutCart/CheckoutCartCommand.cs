using BuildingBlocks.Application.Abstractions;

namespace Cart.Application.Carts.CheckoutCart;

public sealed record CheckoutCartCommand(Guid CartId, string Currency, decimal TotalAmount) : ICommand;
