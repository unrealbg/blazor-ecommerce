namespace Storefront.Web.Services.Api;

public sealed record StorePaymentIntentAction(
    Guid PaymentIntentId,
    string Provider,
    string Status,
    string? ClientSecret,
    bool RequiresAction,
    string? RedirectUrl);
