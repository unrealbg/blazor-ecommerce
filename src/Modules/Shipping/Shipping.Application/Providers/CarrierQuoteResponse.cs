namespace Shipping.Application.Providers;

public sealed record CarrierQuoteResponse(
    decimal? PriceAmount,
    string? Currency,
    int? EstimatedMinDays,
    int? EstimatedMaxDays);
