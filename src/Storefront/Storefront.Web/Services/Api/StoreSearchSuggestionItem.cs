namespace Storefront.Web.Services.Api;

public sealed record StoreSearchSuggestionItem(
    string Name,
    string Slug,
    string? ImageUrl,
    decimal PriceAmount,
    string Currency);
