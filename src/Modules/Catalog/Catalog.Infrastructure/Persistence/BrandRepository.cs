using Catalog.Application.Brands;
using Catalog.Domain.Brands;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class BrandRepository(CatalogDbContext dbContext) : IBrandRepository
{
    public Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        return dbContext.Brands.AddAsync(brand, cancellationToken).AsTask();
    }

    public Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return dbContext.Brands.SingleOrDefaultAsync(brand => brand.Id == brandId, cancellationToken);
    }

    public Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return dbContext.Brands
            .AsNoTracking()
            .SingleOrDefaultAsync(brand => brand.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Brand>> ListAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Brands.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(brand => brand.IsActive);
        }

        return await query
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Brand>> ListByIdsAsync(
        IReadOnlyCollection<Guid> brandIds,
        CancellationToken cancellationToken)
    {
        if (brandIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Brands
            .AsNoTracking()
            .Where(brand => brandIds.Contains(brand.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingBrandId, CancellationToken cancellationToken)
    {
        return dbContext.Brands.AnyAsync(
            brand => brand.Slug == slug &&
                     (excludingBrandId == null || brand.Id != excludingBrandId.Value),
            cancellationToken);
    }
}
