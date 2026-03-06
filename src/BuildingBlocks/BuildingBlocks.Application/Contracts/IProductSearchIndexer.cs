namespace BuildingBlocks.Application.Contracts;

public interface IProductSearchIndexer
{
    Task UpsertAsync(ProductSearchDocumentContract document, CancellationToken cancellationToken);

    Task UpsertManyAsync(IReadOnlyCollection<ProductSearchDocumentContract> documents, CancellationToken cancellationToken);

    Task RebuildAsync(IReadOnlyCollection<ProductSearchDocumentContract> documents, CancellationToken cancellationToken);
}
