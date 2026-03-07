namespace BuildingBlocks.Application.Contracts;

public sealed record CustomerReviewExportRecord(
    Guid ReviewId,
    Guid ProductId,
    int Rating,
    string Title,
    string ReviewText,
    string Status,
    DateTime CreatedAtUtc);