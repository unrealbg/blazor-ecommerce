using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Results;

namespace Shipping.Application.Shipping.QuoteShipping;

public sealed class QuoteShippingCommandHandler(IShippingQuoteCalculator quoteCalculator)
    : ICommandHandler<QuoteShippingCommand, ShippingQuoteResponse>
{
    public async Task<Result<ShippingQuoteResponse>> Handle(
        QuoteShippingCommand request,
        CancellationToken cancellationToken)
    {
        var quotesResult = await quoteCalculator.CalculateQuotesAsync(
            request.CountryCode,
            request.SubtotalAmount,
            request.Currency,
            cancellationToken);

        return quotesResult.IsFailure
            ? Result<ShippingQuoteResponse>.Failure(quotesResult.Error)
            : Result<ShippingQuoteResponse>.Success(new ShippingQuoteResponse(quotesResult.Value));
    }
}
