using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Contracts;
using Catalog.Domain.Products.Events;
using Microsoft.Extensions.Logging;

namespace Catalog.Application.Products.OnProductCreated;

public sealed class ProductCreatedDomainEventHandler(
    IProductCatalogReader productCatalogReader,
    IProductSearchIndexer productSearchIndexer,
    IInventoryStockProvisioner inventoryStockProvisioner,
    ILogger<ProductCreatedDomainEventHandler> logger)
    : IDomainEventHandler<ProductCreated>
{
    public async Task Handle(ProductCreated domainEvent, CancellationToken cancellationToken)
    {
        var product = await productCatalogReader.GetByIdAsync(domainEvent.ProductId, cancellationToken);
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
                product.DefaultCategoryId,
                product.CategorySlug,
                product.CategoryName,
                product.Brand?.Name,
                BuildSearchText(product),
                product.Amount,
                product.Currency,
                product.IsActive,
                product.IsInStock,
                product.ImageUrl,
                now,
                now),
            cancellationToken);

        var defaultVariant = product.Variants.FirstOrDefault(variant => variant.Id == domainEvent.DefaultVariantId)
                             ?? product.Variants.First();
        var ensureStockResult = await inventoryStockProvisioner.EnsureStockItemAsync(
            product.Id,
            defaultVariant.Id,
            defaultVariant.Sku,
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

    private static string BuildSearchText(ProductSnapshot product)
    {
        var variantNames = string.Join(' ', product.Variants.Select(variant => variant.Name).Where(name => !string.IsNullOrWhiteSpace(name)));
        var optionValues = string.Join(
            ' ',
            product.Variants
                .SelectMany(variant => variant.SelectedOptions)
                .Select(option => $"{option.OptionName} {option.Value}")
                .Distinct(StringComparer.OrdinalIgnoreCase));

        return string.Join(
            ' ',
            new[]
            {
                product.Name,
                product.ShortDescription,
                product.Description,
                product.Brand?.Name,
                product.CategoryName,
                variantNames,
                optionValues,
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
