using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Search.Domain.Documents;
using Search.Infrastructure.Persistence;

namespace Search.Infrastructure.Search;

internal sealed class ProductSearchIndexer(SearchDbContext dbContext) : IProductSearchIndexer
{
    private static readonly SemaphoreSlim WriteSemaphore = new(1, 1);

    public async Task UpsertAsync(
        ProductSearchDocumentContract document,
        CancellationToken cancellationToken)
    {
        await WriteSemaphore.WaitAsync(cancellationToken);
        try
        {
            await UpsertInternalAsync(document, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            WriteSemaphore.Release();
        }
    }

    public async Task UpsertManyAsync(
        IReadOnlyCollection<ProductSearchDocumentContract> documents,
        CancellationToken cancellationToken)
    {
        await WriteSemaphore.WaitAsync(cancellationToken);
        try
        {
            foreach (var document in documents)
            {
                await UpsertInternalAsync(document, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            WriteSemaphore.Release();
        }
    }

    public async Task RebuildAsync(
        IReadOnlyCollection<ProductSearchDocumentContract> documents,
        CancellationToken cancellationToken)
    {
        await WriteSemaphore.WaitAsync(cancellationToken);
        try
        {
            var incomingDocuments = documents
                .GroupBy(document => document.ProductId)
                .Select(group => group.Last())
                .ToDictionary(document => document.ProductId);

            var existingDocuments = await dbContext.ProductSearchDocuments.ToListAsync(cancellationToken);
            foreach (var existingDocument in existingDocuments)
            {
                if (!incomingDocuments.TryGetValue(existingDocument.ProductId, out var incomingDocument))
                {
                    dbContext.ProductSearchDocuments.Remove(existingDocument);
                    continue;
                }

                existingDocument.Update(
                    incomingDocument.Slug,
                    incomingDocument.Name,
                    incomingDocument.DescriptionText,
                    incomingDocument.CategorySlug,
                    incomingDocument.CategoryName,
                    incomingDocument.Brand,
                    incomingDocument.PriceAmount,
                    incomingDocument.Currency,
                    incomingDocument.IsActive,
                    incomingDocument.IsInStock,
                    incomingDocument.ImageUrl,
                    incomingDocument.UpdatedAtUtc,
                    existingDocument.PopularityScore);

                incomingDocuments.Remove(existingDocument.ProductId);
            }

            foreach (var pendingDocument in incomingDocuments.Values)
            {
                dbContext.ProductSearchDocuments.Add(Map(pendingDocument));
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            WriteSemaphore.Release();
        }
    }

    private async Task UpsertInternalAsync(
        ProductSearchDocumentContract document,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.ProductSearchDocuments
            .SingleOrDefaultAsync(item => item.ProductId == document.ProductId, cancellationToken);

        if (existing is null)
        {
            dbContext.ProductSearchDocuments.Add(Map(document));
            return;
        }

        existing.Update(
            document.Slug,
            document.Name,
            document.DescriptionText,
            document.CategorySlug,
            document.CategoryName,
            document.Brand,
            document.PriceAmount,
            document.Currency,
            document.IsActive,
            document.IsInStock,
            document.ImageUrl,
            document.UpdatedAtUtc,
            existing.PopularityScore);
    }

    private ProductSearchDocument Map(ProductSearchDocumentContract document)
    {
        return ProductSearchDocument.Create(
            document.ProductId,
            document.Slug,
            document.Name,
            document.DescriptionText,
            document.CategorySlug,
            document.CategoryName,
            document.Brand,
            document.PriceAmount,
            document.Currency,
            document.IsActive,
            document.IsInStock,
            document.ImageUrl,
            document.CreatedAtUtc,
            document.UpdatedAtUtc);
    }
}
