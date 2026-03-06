namespace Shipping.Application.Providers;

public sealed record CarrierQuoteRequest(
    string CountryCode,
    decimal SubtotalAmount,
    string Currency);
