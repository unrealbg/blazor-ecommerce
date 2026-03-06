using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Catalog.Domain.Products.Events;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Products.OnProductSlugChanged;

public sealed class ProductSlugChangedSearchIndexDomainEventHandler(
    IProductRepository productRepository,
    IProductSearchIndexer productSearchIndexer,
    ILogger<ProductSlugChangedSearchIndexDomainEventHandler> logger)
    : IDomainEventHandler<ProductSlugChanged>
{
    public async Task Handle(ProductSlugChanged domainEvent, CancellationToken cancellationToken)
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

        logger.LogInformation(
            "Search index document upserted after product slug change. ProductId: {ProductId}, NewSlug: {Slug}",
            product.Id,
            product.Slug);
    }
}
