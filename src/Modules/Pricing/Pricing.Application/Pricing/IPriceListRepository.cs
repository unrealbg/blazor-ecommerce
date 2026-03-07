using Pricing.Domain.PriceLists;

namespace Pricing.Application.Pricing;

public interface IPriceListRepository
{
    Task AddAsync(PriceList priceList, CancellationToken cancellationToken);

    Task<PriceList?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PriceList?> GetByCodeAsync(string code, CancellationToken cancellationToken);

    Task<PriceList?> GetDefaultAsync(string currency, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PriceList>> ListAsync(CancellationToken cancellationToken);
}
