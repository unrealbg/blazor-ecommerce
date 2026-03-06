namespace Search.Application.Search;

public sealed record SearchSuggestionItem(
    string Name,
    string Slug,
    string? ImageUrl,
    decimal PriceAmount,
    string Currency);
