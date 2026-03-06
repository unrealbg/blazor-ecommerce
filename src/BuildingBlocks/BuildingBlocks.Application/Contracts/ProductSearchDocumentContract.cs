namespace BuildingBlocks.Application.Contracts;

public sealed record ProductSearchDocumentContract(
    Guid ProductId,
    string Slug,
    string Name,
    string? DescriptionText,
    string? CategorySlug,
    string? CategoryName,
    string? Brand,
    decimal PriceAmount,
    string Currency,
    bool IsActive,
    bool IsInStock,
    string? ImageUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
