namespace Backoffice.Application.Backoffice;

public sealed record BackofficePaymentSummaryDto(
    Guid Id,
    string Provider,
    string Status,
    decimal Amount,
    string Currency,
    string? ProviderPaymentIntentId,
    string? FailureCode,
    string? FailureMessage,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<BackofficePaymentTransactionDto> Transactions);
