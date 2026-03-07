namespace Backoffice.Application.Backoffice;

public sealed record BackofficePaymentTransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    string Status,
    string? ProviderTransactionId,
    string? RawReference,
    DateTime CreatedAtUtc);
