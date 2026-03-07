namespace Backoffice.Application.Backoffice;

public sealed record BackofficeCustomerActivityItemDto(
    string Type,
    string Summary,
    DateTime OccurredAtUtc,
    string? TargetType,
    string? TargetId);
