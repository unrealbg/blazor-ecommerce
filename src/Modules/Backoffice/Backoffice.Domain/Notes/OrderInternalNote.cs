using BuildingBlocks.Domain.Primitives;

namespace Backoffice.Domain.Notes;

public sealed class OrderInternalNote : Entity<Guid>
{
    private OrderInternalNote()
    {
    }

    private OrderInternalNote(
        Guid id,
        Guid orderId,
        string note,
        DateTime createdAtUtc,
        string? authorUserId,
        string? authorEmail,
        string? authorDisplayName)
    {
        Id = id;
        OrderId = orderId;
        Note = note;
        CreatedAtUtc = createdAtUtc;
        AuthorUserId = authorUserId;
        AuthorEmail = authorEmail;
        AuthorDisplayName = authorDisplayName;
    }

    public Guid OrderId { get; private set; }

    public string Note { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public string? AuthorUserId { get; private set; }

    public string? AuthorEmail { get; private set; }

    public string? AuthorDisplayName { get; private set; }

    public static OrderInternalNote Create(
        Guid orderId,
        string note,
        DateTime createdAtUtc,
        string? authorUserId,
        string? authorEmail,
        string? authorDisplayName)
    {
        return new OrderInternalNote(
            Guid.NewGuid(),
            orderId,
            note.Trim(),
            createdAtUtc,
            Normalize(authorUserId),
            Normalize(authorEmail),
            Normalize(authorDisplayName));
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
