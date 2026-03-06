namespace Storefront.Web.Services.Api;

public sealed record StoreCheckoutRequest(
    string CartSessionId,
    string Email,
    StoreCheckoutAddress ShippingAddress,
    StoreCheckoutAddress BillingAddress);
