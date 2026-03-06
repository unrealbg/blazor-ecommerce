using BuildingBlocks.Application.Abstractions;

namespace Orders.Application.Orders.Checkout;

public sealed record CheckoutWithProfileCommand(
    string CartSessionId,
    string Email,
    CheckoutAddressInput ShippingAddress,
    CheckoutAddressInput BillingAddress,
    string IdempotencyKey,
    Guid? UserId) : ICommand<Guid>;
