namespace BuildingBlocks.Application.Contracts;

public sealed record CategoryBreadcrumbSnapshot(
    Guid Id,
    string Name,
    string Slug);
