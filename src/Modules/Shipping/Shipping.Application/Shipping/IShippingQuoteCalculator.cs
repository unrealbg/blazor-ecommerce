using BuildingBlocks.Domain.Results;

namespace Shipping.Application.Shipping;

public interface IShippingQuoteCalculator
{
    Task<Result<IReadOnlyCollection<ShippingQuoteMethodDto>>> CalculateQuotesAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        CancellationToken cancellationToken);
}
