namespace Backoffice.Application.Backoffice;

public sealed record BackofficeOperationalAlertDto(
    string Code,
    string Severity,
    string Summary,
    string? Details,
    DateTime OccurredAtUtc,
    IReadOnlyDictionary<string, string?> Context);