using Microsoft.EntityFrameworkCore;
using Pricing.Application.Pricing;
using Pricing.Domain.PriceLists;

namespace Pricing.Infrastructure.Persistence;

internal sealed class PriceListRepository(PricingDbContext dbContext) : IPriceListRepository
{
    public Task AddAsync(PriceList priceList, CancellationToken cancellationToken)
    {
        return dbContext.PriceLists.AddAsync(priceList, cancellationToken).AsTask();
    }

    public Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.PriceLists.SingleOrDefaultAsync(priceList => priceList.Id == id, cancellationToken);
    }

    public Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var normalizedCode = code.Trim().ToLowerInvariant();
        return dbContext.PriceLists.SingleOrDefaultAsync(priceList => priceList.Code == normalizedCode, cancellationToken);
    }

    public Task<PriceList?> GetDefaultAsync(string currency, CancellationToken cancellationToken)
    {
        var normalizedCurrency = currency.Trim().ToUpperInvariant();
        return dbContext.PriceLists
            .Where(priceList => priceList.Currency == normalizedCurrency && priceList.IsDefault && priceList.IsActive)
            .OrderByDescending(priceList => priceList.Priority)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PriceList>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.PriceLists
            .OrderByDescending(priceList => priceList.IsDefault)
            .ThenByDescending(priceList => priceList.Priority)
            .ThenBy(priceList => priceList.Name)
            .ToListAsync(cancellationToken);
    }
}
