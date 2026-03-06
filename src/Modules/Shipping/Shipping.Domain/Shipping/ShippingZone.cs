using System.Text.Json;
using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Shipping.Domain.Shipping;

public sealed class ShippingZone : AggregateRoot<Guid>
{
    private ShippingZone()
    {
    }

    private ShippingZone(Guid id, string code, string name, string countryCodesJson, DateTime createdAtUtc)
    {
        Id = id;
        Code = code;
        Name = name;
        CountryCodesJson = countryCodesJson;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
        RowVersion = 0;
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string CountryCodesJson { get; private set; } = "[]";

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public long RowVersion { get; private set; }

    public static Result<ShippingZone> Create(
        string code,
        string name,
        IReadOnlyCollection<string> countryCodes,
        DateTime createdAtUtc)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.IsFailure)
        {
            return Result<ShippingZone>.Failure(normalizedCode.Error);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result<ShippingZone>.Failure(new Error(
                "shipping.zone.name.required",
                "Shipping zone name is required."));
        }

        var normalizeCountriesResult = NormalizeCountries(countryCodes);
        if (normalizeCountriesResult.IsFailure)
        {
            return Result<ShippingZone>.Failure(normalizeCountriesResult.Error);
        }

        return Result<ShippingZone>.Success(new ShippingZone(
            Guid.NewGuid(),
            normalizedCode.Value,
            name.Trim(),
            JsonSerializer.Serialize(normalizeCountriesResult.Value),
            createdAtUtc));
    }

    public Result Update(string name, IReadOnlyCollection<string> countryCodes, bool isActive, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(new Error(
                "shipping.zone.name.required",
                "Shipping zone name is required."));
        }

        var normalizeCountriesResult = NormalizeCountries(countryCodes);
        if (normalizeCountriesResult.IsFailure)
        {
            return normalizeCountriesResult;
        }

        Name = name.Trim();
        CountryCodesJson = JsonSerializer.Serialize(normalizeCountriesResult.Value);
        IsActive = isActive;
        Touch(updatedAtUtc);

        return Result.Success();
    }

    public bool AppliesToCountry(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return false;
        }

        IReadOnlyCollection<string>? countries;
        try
        {
            countries = JsonSerializer.Deserialize<IReadOnlyCollection<string>>(CountryCodesJson);
        }
        catch (JsonException)
        {
            return false;
        }

        if (countries is null)
        {
            return false;
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        return countries.Any(country => string.Equals(country, normalized, StringComparison.Ordinal));
    }

    private static Result<string> NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Result<string>.Failure(new Error(
                "shipping.zone.code.required",
                "Shipping zone code is required."));
        }

        return Result<string>.Success(code.Trim().ToLowerInvariant());
    }

    private static Result<IReadOnlyCollection<string>> NormalizeCountries(IReadOnlyCollection<string> countryCodes)
    {
        if (countryCodes.Count == 0)
        {
            return Result<IReadOnlyCollection<string>>.Failure(new Error(
                "shipping.zone.country_codes.required",
                "Shipping zone must include at least one country code."));
        }

        var normalized = countryCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            return Result<IReadOnlyCollection<string>>.Failure(new Error(
                "shipping.zone.country_codes.required",
                "Shipping zone must include at least one country code."));
        }

        if (normalized.Any(code => code.Length != 2))
        {
            return Result<IReadOnlyCollection<string>>.Failure(new Error(
                "shipping.zone.country_codes.invalid",
                "Shipping zone country codes must use 2-letter format."));
        }

        return Result<IReadOnlyCollection<string>>.Success(normalized);
    }

    private void Touch(DateTime utcNow)
    {
        UpdatedAtUtc = utcNow;
        RowVersion++;
    }
}
