namespace Payments.Application.Payments;

public sealed record PaymentTransactionDto(
    Guid Id,
    string Type,
    string? ProviderTransactionId,
    decimal Amount,
    string Currency,
    string Status,
    string? RawReference,
    DateTime CreatedAtUtc,
    string? MetadataJson);
