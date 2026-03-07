using Catalog.Application.Products;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class ProductRepository(CatalogDbContext dbContext) : IProductRepository
{
    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        return dbContext.Products.AddAsync(product, cancellationToken).AsTask();
    }

    public Task<Product?> GetByIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return QueryProducts()
            .SingleOrDefaultAsync(product => product.Id == productId, cancellationToken);
    }

    public Task<Product?> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken)
    {
        return QueryProducts()
            .SingleOrDefaultAsync(
                product => product.Variants.Any(variant => variant.Id == variantId),
                cancellationToken);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return QueryProducts()
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> ListAsync(CancellationToken cancellationToken)
    {
        return await QueryProducts()
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        return await QueryProducts()
            .AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingProductId, CancellationToken cancellationToken)
    {
        return dbContext.Products.AnyAsync(
            product => product.Slug == slug &&
                       (excludingProductId == null || product.Id != excludingProductId.Value),
            cancellationToken);
    }

    public Task<bool> SkuExistsAsync(string sku, Guid? excludingVariantId, CancellationToken cancellationToken)
    {
        return dbContext.ProductVariants.AnyAsync(
            variant => variant.Sku == sku &&
                       (excludingVariantId == null || variant.Id != excludingVariantId.Value),
            cancellationToken);
    }

    private IQueryable<Product> QueryProducts()
    {
        return dbContext.Products
            .Include(product => product.Categories)
            .Include(product => product.Variants)
                .ThenInclude(variant => variant.OptionAssignments)
            .Include(product => product.Options)
                .ThenInclude(option => option.Values)
            .Include(product => product.Attributes)
            .Include(product => product.Images)
            .Include(product => product.Relations)
            .AsSplitQuery();
    }
}
