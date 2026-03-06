using BuildingBlocks.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductCatalogReader(CatalogDbContext dbContext) : IProductCatalogReader
{
    public async Task<ProductSnapshot?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(product => product.Id == productId)
            .Select(product => new ProductSnapshot(
                product.Id,
                product.Name,
                product.Description,
                product.Price.Currency,
                product.Price.Amount,
                product.IsActive,
                product.IsInStock,
                product.Slug,
                product.Brand,
                product.CategorySlug,
                product.CategoryName,
                product.ImageUrl,
                DateTime.UtcNow,
                DateTime.UtcNow))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<ProductSnapshot>> ListAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .Select(product => new ProductSnapshot(
                product.Id,
                product.Name,
                product.Description,
                product.Price.Currency,
                product.Price.Amount,
                product.IsActive,
                product.IsInStock,
                product.Slug,
                product.Brand,
                product.CategorySlug,
                product.CategoryName,
                product.ImageUrl,
                DateTime.UtcNow,
                DateTime.UtcNow))
            .ToListAsync(cancellationToken);
    }
}
