namespace Payments.Application.Payments;

public sealed record PaymentIntentDetailsDto(
    Guid Id,
    Guid OrderId,
    Guid? CustomerId,
    string Provider,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderPaymentIntentId,
    string? ClientSecret,
    string? FailureCode,
    string? FailureMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<PaymentTransactionDto> Transactions);
