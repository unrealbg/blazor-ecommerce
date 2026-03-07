using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Logging;

namespace Search.Application.Search;

public sealed class RebuildSearchIndexCommandHandler(
    IProductCatalogReader productCatalogReader,
    IProductSearchIndexer productSearchIndexer,
    ILogger<RebuildSearchIndexCommandHandler> logger)
    : ICommandHandler<RebuildSearchIndexCommand, int>
{
    public async Task<Result<int>> Handle(RebuildSearchIndexCommand request, CancellationToken cancellationToken)
    {
        var products = await productCatalogReader.ListAllAsync(cancellationToken);

        var searchDocuments = products
            .Where(product => !string.IsNullOrWhiteSpace(product.Slug))
            .Select(MapToSearchDocument)
            .ToArray();

        await productSearchIndexer.RebuildAsync(searchDocuments, cancellationToken);

        logger.LogInformation("Search index rebuild completed with {DocumentsCount} documents", searchDocuments.Length);

        return Result<int>.Success(searchDocuments.Length);
    }

    private static ProductSearchDocumentContract MapToSearchDocument(ProductSnapshot snapshot)
    {
        var now = DateTime.UtcNow;

        return new ProductSearchDocumentContract(
            snapshot.Id,
            snapshot.Slug,
            snapshot.Name,
            snapshot.Description,
            snapshot.DefaultCategoryId,
            snapshot.CategorySlug,
            snapshot.CategoryName,
            snapshot.Brand?.Name,
            string.Join(
                ' ',
                snapshot.Variants
                    .SelectMany(variant => variant.SelectedOptions)
                    .Select(option => $"{option.OptionName} {option.Value}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)),
            snapshot.Amount,
            snapshot.Currency,
            snapshot.IsActive,
            snapshot.IsInStock,
            snapshot.ImageUrl,
            snapshot.CreatedAtUtc == default ? now : snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc == default ? now : snapshot.UpdatedAtUtc);
    }
}
