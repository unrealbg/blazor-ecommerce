using System.Text.Json;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Results;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Shipping.Application.Shipping;
using Shipping.Domain.Shipping;

namespace Shipping.Infrastructure.Shipping;

internal sealed class ShippingQuoteService(
    IShippingMethodRepository shippingMethodRepository,
    IShippingZoneRepository shippingZoneRepository,
    IShippingRateRuleRepository shippingRateRuleRepository,
    IDistributedCache distributedCache,
    IOptions<ShippingModuleOptions> options)
    : IShippingQuoteService, IShippingQuoteCalculator
{
    private const string CachePrefix = "shipping:quotes:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ShippingModuleOptions options = options.Value;

    public async Task<Result<ShippingQuoteSelection>> ResolveQuoteAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        string? shippingMethodCode,
        CancellationToken cancellationToken)
    {
        var quotesResult = await CalculateQuotesAsync(countryCode, subtotalAmount, currency, cancellationToken);
        if (quotesResult.IsFailure)
        {
            return Result<ShippingQuoteSelection>.Failure(quotesResult.Error);
        }

        var quotes = quotesResult.Value.ToList();
        if (quotes.Count == 0)
        {
            return Result<ShippingQuoteSelection>.Failure(new Error(
                "shipping.no_methods_available",
                "No shipping methods are available for the destination."));
        }

        ShippingQuoteMethodDto selectedQuote;
        if (string.IsNullOrWhiteSpace(shippingMethodCode))
        {
            selectedQuote = quotes[0];
        }
        else
        {
            var normalizedCode = shippingMethodCode.Trim().ToLowerInvariant();
            selectedQuote = quotes.FirstOrDefault(quote => quote.Code == normalizedCode)!;
            if (selectedQuote is null)
            {
                return Result<ShippingQuoteSelection>.Failure(new Error(
                    "shipping.method.not_applicable",
                    "Selected shipping method is not available for the destination."));
            }
        }

        return Result<ShippingQuoteSelection>.Success(new ShippingQuoteSelection(
            selectedQuote.Id,
            selectedQuote.Code,
            selectedQuote.Name,
            selectedQuote.PriceAmount,
            selectedQuote.Currency,
            selectedQuote.EstimatedMinDays,
            selectedQuote.EstimatedMaxDays,
            selectedQuote.IsFreeShipping));
    }

    public async Task<Result<IReadOnlyCollection<ShippingQuoteMethodDto>>> CalculateQuotesAsync(
        string countryCode,
        decimal subtotalAmount,
        string currency,
        CancellationToken cancellationToken)
    {
        var normalizedCountryCodeResult = NormalizeCountryCode(countryCode);
        if (normalizedCountryCodeResult.IsFailure)
        {
            return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Failure(normalizedCountryCodeResult.Error);
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? this.options.DefaultCurrency
            : currency.Trim().ToUpperInvariant();

        if (subtotalAmount < 0m)
        {
            return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Failure(new Error(
                "shipping.subtotal.invalid",
                "Subtotal amount cannot be negative."));
        }

        var cacheKey = BuildCacheKey(normalizedCountryCodeResult.Value, subtotalAmount, normalizedCurrency);
        var cachedPayload = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedPayload))
        {
            try
            {
                var cachedQuotes = JsonSerializer.Deserialize<IReadOnlyCollection<ShippingQuoteMethodDto>>(
                    cachedPayload,
                    JsonOptions);
                if (cachedQuotes is not null)
                {
                    return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Success(cachedQuotes);
                }
            }
            catch (JsonException)
            {
                // Ignore malformed cache payloads and recompute.
            }
        }

        var activeZones = await shippingZoneRepository.ListAsync(activeOnly: true, cancellationToken);
        var zone = activeZones.FirstOrDefault(item => item.AppliesToCountry(normalizedCountryCodeResult.Value));
        if (zone is null)
        {
            return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Failure(new Error(
                "shipping.zone.not_found",
                "No shipping zone matches the destination country."));
        }

        var activeMethods = await shippingMethodRepository.ListAsync(activeOnly: true, cancellationToken);
        if (activeMethods.Count == 0)
        {
            return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Failure(new Error(
                "shipping.no_methods_available",
                "No active shipping methods are configured."));
        }

        var activeRules = await shippingRateRuleRepository.ListByZoneAsync(zone.Id, activeOnly: true, cancellationToken);
        var quoteMethods = new List<ShippingQuoteMethodDto>(activeMethods.Count);

        foreach (var method in activeMethods)
        {
            if (!string.Equals(method.Currency, normalizedCurrency, StringComparison.Ordinal))
            {
                continue;
            }

            var matchingRule = activeRules
                .Where(rule =>
                    rule.ShippingMethodId == method.Id &&
                    string.Equals(rule.Currency, normalizedCurrency, StringComparison.Ordinal) &&
                    rule.Matches(subtotalAmount, totalWeightKg: null))
                .OrderByDescending(rule => rule.MinOrderAmount ?? 0m)
                .ThenBy(rule => rule.MaxOrderAmount ?? decimal.MaxValue)
                .ThenBy(rule => rule.PriceAmount)
                .FirstOrDefault();

            var resolvedPrice = matchingRule is null
                ? method.BasePriceAmount
                : matchingRule.ResolvePrice(subtotalAmount);

            quoteMethods.Add(new ShippingQuoteMethodDto(
                method.Id,
                method.Code,
                method.Name,
                method.Description,
                resolvedPrice,
                normalizedCurrency,
                method.EstimatedMinDays,
                method.EstimatedMaxDays,
                IsFreeShipping: resolvedPrice == 0m));
        }

        var methodPriority = activeMethods.ToDictionary(method => method.Id, method => method.Priority);
        var orderedMethods = quoteMethods
            .OrderBy(method => methodPriority.TryGetValue(method.Id, out var priority) ? priority : int.MaxValue)
            .ThenBy(method => method.PriceAmount)
            .ThenBy(method => method.Name)
            .ToList();

        await distributedCache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(orderedMethods, JsonOptions),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = this.options.QuoteCacheTtl,
            },
            cancellationToken);

        return Result<IReadOnlyCollection<ShippingQuoteMethodDto>>.Success(orderedMethods);
    }

    private static Result<string> NormalizeCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return Result<string>.Failure(new Error(
                "shipping.country.required",
                "Country code is required."));
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        if (normalized.Length != 2)
        {
            return Result<string>.Failure(new Error(
                "shipping.country.invalid",
                "Country code must use a 2-letter format."));
        }

        return Result<string>.Success(normalized);
    }

    private static string BuildCacheKey(string countryCode, decimal subtotalAmount, string currency)
    {
        var normalizedSubtotal = decimal.Round(subtotalAmount, 2, MidpointRounding.AwayFromZero);
        return $"{CachePrefix}{countryCode}:{currency}:{normalizedSubtotal:0.00}";
    }
}
