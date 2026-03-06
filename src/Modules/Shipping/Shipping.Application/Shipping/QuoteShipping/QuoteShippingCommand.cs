using BuildingBlocks.Application.Abstractions;

namespace Shipping.Application.Shipping.QuoteShipping;

public sealed record QuoteShippingCommand(
    string CountryCode,
    decimal SubtotalAmount,
    string Currency) : ICommand<ShippingQuoteResponse>;
