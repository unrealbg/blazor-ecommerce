using Catalog.Domain.Brands;

namespace Catalog.Application.Brands;

public interface IBrandRepository
{
    Task AddAsync(Brand brand, CancellationToken cancellationToken);

    Task<Brand?> GetByIdAsync(Guid brandId, CancellationToken cancellationToken);

    Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Brand>> ListAsync(bool activeOnly, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Brand>> ListByIdsAsync(
        IReadOnlyCollection<Guid> brandIds,
        CancellationToken cancellationToken);

    Task<bool> SlugExistsAsync(string slug, Guid? excludingBrandId, CancellationToken cancellationToken);
}
