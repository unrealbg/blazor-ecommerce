using BuildingBlocks.Application.Contracts;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullProductSearchIndexer : IProductSearchIndexer
{
    public Task UpsertAsync(ProductSearchDocumentContract document, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task UpsertManyAsync(IReadOnlyCollection<ProductSearchDocumentContract> documents, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RebuildAsync(IReadOnlyCollection<ProductSearchDocumentContract> documents, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
