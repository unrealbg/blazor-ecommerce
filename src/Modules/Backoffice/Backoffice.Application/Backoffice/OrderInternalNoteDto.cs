namespace Backoffice.Application.Backoffice;

public sealed record OrderInternalNoteDto(
    Guid Id,
    Guid OrderId,
    string Note,
    DateTime CreatedAtUtc,
    string? AuthorUserId,
    string? AuthorEmail,
    string? AuthorDisplayName);
