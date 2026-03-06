namespace Search.Application.Search;

public sealed record SearchSuggestionsResponse(
    string Query,
    IReadOnlyCollection<SearchSuggestionItem> Suggestions);
