namespace BuildingBlocks.Application.Contracts;

public sealed record ProductSearchDocumentContract(
    Guid ProductId,
    string Slug,
    string Name,
    string? DescriptionText,
    Guid? CategoryId,
    string? CategorySlug,
    string? CategoryName,
    string? Brand,
    string? SearchText,
    decimal PriceAmount,
    string Currency,
    bool IsActive,
    bool IsInStock,
    string? ImageUrl,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
