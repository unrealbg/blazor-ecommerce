using Catalog.Domain.Categories;

namespace Catalog.Application.Categories;

public interface ICategoryRepository
{
    Task AddAsync(Category category, CancellationToken cancellationToken);

    Task<Category?> GetByIdAsync(Guid categoryId, CancellationToken cancellationToken);

    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Category>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Category>> ListByIdsAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingCategoryId, CancellationToken cancellationToken);
}
