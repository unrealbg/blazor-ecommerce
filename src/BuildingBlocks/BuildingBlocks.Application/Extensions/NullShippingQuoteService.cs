using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;

namespace BuildingBlocks.Application.Extensions;

internal sealed class NullShippingQuoteService : IShippingQuoteService
{
    public Task<Result<ShippingQuoteSelection>> ResolveQuoteAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        string? shippingMethodCode,
        CancellationToken cancellationToken)
    {
        var code = string.IsNullOrWhiteSpace(shippingMethodCode) ? "standard" : shippingMethodCode.Trim().ToLowerInvariant();

        return Task.FromResult(Result<ShippingQuoteSelection>.Success(new ShippingQuoteSelection(
            Guid.Empty,
            code,
            "Standard Delivery",
            0m,
            string.IsNullOrWhiteSpace(currency) ? "EUR" : currency.Trim().ToUpperInvariant(),
            EstimatedMinDays: null,
            EstimatedMaxDays: null,
            IsFreeShipping: true)));
    }
}
