using Catalog.Application.Categories;
using Catalog.Domain.Categories;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

internal sealed class CategoryRepository(CatalogDbContext dbContext) : ICategoryRepository
{
    public Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AddAsync(category, cancellationToken).AsTask();
    }

    public Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.SingleOrDefaultAsync(category => category.Id == categoryId, cancellationToken);
    }

    public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Category>> ListAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Categories.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(category => category.IsActive);
        }

        return await query
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Category>> ListByIdsAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        if (categoryIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Categories
            .AsNoTracking()
            .Where(category => categoryIds.Contains(category.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingCategoryId, CancellationToken cancellationToken)
    {
        return dbContext.Categories.AnyAsync(
            category => category.Slug == slug &&
                        (excludingCategoryId == null || category.Id != excludingCategoryId.Value),
            cancellationToken);
    }
}
