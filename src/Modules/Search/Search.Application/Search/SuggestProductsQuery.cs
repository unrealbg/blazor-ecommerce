using BuildingBlocks.Application.Abstractions;

namespace Search.Application.Search;

public sealed record SuggestProductsQuery(string Query, int Limit = 8) : IQuery<SearchSuggestionsResponse>;
