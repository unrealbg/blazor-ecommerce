using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Catalog.Domain.Products.Events;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Products.OnProductCreated;

public sealed class ProductCreatedDomainEventHandler(
    IProductRepository productRepository,
    IProductSearchIndexer productSearchIndexer,
    IInventoryStockProvisioner inventoryStockProvisioner,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : IDomainEventHandler<ProductCreated>
{
    public async Task Handle(ProductCreated domainEvent, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(domainEvent.ProductId, cancellationToken);
        if (product is null)
        {
            logger.LogWarning(
                "Skipping search index update because product {ProductId} was not found.",
                domainEvent.ProductId);
            return;
        }

        var now = DateTime.UtcNow;
        await productSearchIndexer.UpsertAsync(
            new ProductSearchDocumentContract(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.CategorySlug,
                product.CategoryName,
                product.Brand,
                product.Price.Amount,
                product.Price.Currency,
                product.IsActive,
                product.IsInStock,
                product.ImageUrl,
                now,
                now),
            cancellationToken);

        var ensureStockResult = await inventoryStockProvisioner.EnsureStockItemAsync(
            product.Id,
            product.Sku,
            product.IsInStock ? 100 : 0,
            isTracked: true,
            allowBackorder: false,
            cancellationToken);

        if (ensureStockResult.IsFailure)
        {
            logger.LogWarning(
                "Inventory stock item provisioning failed for product {ProductId}. Code: {Code}. Message: {Message}",
                product.Id,
                ensureStockResult.Error.Code,
                ensureStockResult.Error.Message);
        }

        logger.LogInformation(
            "Search index document upserted after product creation. ProductId: {ProductId}",
            product.Id);
    }
}
