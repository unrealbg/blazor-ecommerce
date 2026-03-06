namespace Search.Application.Search;

public interface ISearchProvider
{
    Task<ProductSearchResult> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<SearchSuggestionItem>> SuggestAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
}
