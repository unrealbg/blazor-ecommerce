using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Inventory.Domain.Stock.Events;
using Microsoft.Extensions.Logging;

namespace Inventory.Application.Stock.OnStockAvailabilityChanged;

public sealed class StockAvailabilityChangedDomainEventHandler(
    IProductCatalogReader productCatalogReader,
    IProductSearchIndexer productSearchIndexer,
    ILogger<StockAvailabilityChangedDomainEventHandler> logger)
    : IDomainEventHandler<StockAvailabilityChanged>
{
    public async Task Handle(StockAvailabilityChanged domainEvent, CancellationToken cancellationToken)
    {
        var product = await productCatalogReader.GetByIdAsync(domainEvent.ProductId, cancellationToken);
        if (product is null)
        {
            logger.LogWarning(
                "Skipping search update for stock event because product {ProductId} was not found.",
                domainEvent.ProductId);
            return;
        }

        await productSearchIndexer.UpsertAsync(
            new ProductSearchDocumentContract(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.CategorySlug,
                product.CategoryName,
                product.Brand,
                product.Amount,
                product.Currency,
                product.IsActive,
                domainEvent.IsInStock,
                product.ImageUrl,
                product.CreatedAtUtc == default ? DateTime.UtcNow : product.CreatedAtUtc,
                DateTime.UtcNow),
            cancellationToken);

        logger.LogInformation(
            "Search index document updated after stock availability change. ProductId: {ProductId}, IsInStock: {IsInStock}",
            domainEvent.ProductId,
            domainEvent.IsInStock);
    }
}
