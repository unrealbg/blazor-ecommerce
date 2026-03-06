namespace Storefront.Web.Services.Api;

public sealed record StoreSearchSuggestionsResponse(
    string Query,
    IReadOnlyCollection<StoreSearchSuggestionItem> Suggestions);
