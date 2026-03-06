namespace Storefront.Web.Services.Api;

public sealed record StoreCheckoutRequest(
    string CartSessionId,
    string Email,
    string? ShippingMethodCode,
    StoreCheckoutAddress ShippingAddress,
    StoreCheckoutAddress BillingAddress);
