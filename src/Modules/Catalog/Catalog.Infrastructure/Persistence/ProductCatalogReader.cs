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
                product.IsActive))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
