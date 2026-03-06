namespace Payments.Application.Payments;

public sealed record PaymentIntentSummaryDto(
    Guid Id,
    Guid OrderId,
    string Provider,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderPaymentIntentId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc);
