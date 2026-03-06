using BuildingBlocks.Application.Abstractions;

namespace Search.Application.Search;

public sealed record RebuildSearchIndexCommand : ICommand<int>;
