namespace Search.Application.Search;

public static class SearchSortOptions
{
    public const string Relevance = "relevance";
    public const string Popular = "popular";
    public const string Newest = "newest";
    public const string PriceAscending = "price_asc";
    public const string PriceDescending = "price_desc";
    public const string NameAscending = "name_asc";

    public static readonly IReadOnlyCollection<string> SupportedValues =
    [
        Relevance,
        Popular,
        Newest,
        PriceAscending,
        PriceDescending,
        NameAscending,
    ];

    public static string Normalize(string? sort, bool hasQuery)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return hasQuery ? Relevance : Popular;
        }

        var normalized = sort.Trim().ToLowerInvariant();
        if (!SupportedValues.Contains(normalized, StringComparer.Ordinal))
        {
            return hasQuery ? Relevance : Popular;
        }

        return normalized;
    }
}
