using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Contracts;

public interface IShippingQuoteService
{
    Task<Result<ShippingQuoteSelection>> ResolveQuoteAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        string? shippingMethodCode,
        CancellationToken cancellationToken);
}
