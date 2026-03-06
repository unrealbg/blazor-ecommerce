using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductCatalogReader(
    CatalogDbContext dbContext,
    IInventoryAvailabilityReader inventoryAvailabilityReader)
    : IProductCatalogReader
{
    public async Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
        if (product is null)
        {
            return null;
        }

        var availability = await inventoryAvailabilityReader.GetByProductIdAsync(product.Id, cancellationToken);

        return new ProductSnapshot(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Currency,
            product.Price.Amount,
            product.IsActive,
            availability?.IsInStock ?? product.IsInStock,
            product.Slug,
            product.Brand,
            product.CategorySlug,
            product.CategoryName,
            product.ImageUrl,
            DateTime.UtcNow,
            DateTime.UtcNow,
            availability?.IsTracked ?? false,
            availability?.AllowBackorder ?? false,
            availability?.AvailableQuantity,
            product.Sku);
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> ListAllAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);

        var availabilityByProduct = await inventoryAvailabilityReader.GetByProductIdsAsync(
            products.Select(product => product.Id).ToArray(),
            cancellationToken);

        return products
            .Select(product =>
            {
                var hasAvailability = availabilityByProduct.TryGetValue(product.Id, out var availability);

                return new ProductSnapshot(
                    product.Id,
                    product.Name,
                    product.Description,
                    product.Price.Currency,
                    product.Price.Amount,
                    product.IsActive,
                    hasAvailability ? availability!.IsInStock : product.IsInStock,
                    product.Slug,
                    product.Brand,
                    product.CategorySlug,
                    product.CategoryName,
                    product.ImageUrl,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    hasAvailability && availability!.IsTracked,
                    hasAvailability && availability!.AllowBackorder,
                    hasAvailability ? availability!.AvailableQuantity : null,
                    product.Sku);
            })
            .ToList();
    }
}
