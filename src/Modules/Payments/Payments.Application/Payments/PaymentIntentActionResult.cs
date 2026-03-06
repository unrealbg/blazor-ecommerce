namespace Payments.Application.Payments;

public sealed record PaymentIntentActionResult(
    Guid PaymentIntentId,
    string Provider,
    string Status,
    string? ClientSecret,
    bool RequiresAction,
    string? RedirectUrl);
