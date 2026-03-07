namespace BuildingBlocks.Application.Contracts;

public sealed record CustomerQuestionExportRecord(
    Guid QuestionId,
    Guid ProductId,
    string QuestionText,
    string Status,
    DateTime CreatedAtUtc);