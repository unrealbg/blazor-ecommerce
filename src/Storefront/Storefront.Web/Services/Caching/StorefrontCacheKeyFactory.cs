using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Storefront.Web.Services.Api;

namespace Storefront.Web.Services.Caching;

internal static class StorefrontCacheKeyFactory
{
    public static string Products() => "storefront:catalog:index";

    public static string Product(string slug) => $"storefront:product:{Normalize(slug)}";

    public static string ReviewSummary(Guid productId) => $"reviews:summary:{productId:D}";

    public static string Search(StoreSearchProductsRequest request)
    {
        var builder = new StringBuilder();
        builder.Append(request.Query?.Trim().ToLowerInvariant() ?? string.Empty).Append('|');
        builder.Append(request.CategorySlug?.Trim().ToLowerInvariant() ?? string.Empty).Append('|');

        foreach (var brand in (request.Brands ?? []).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(brand.Trim().ToLowerInvariant()).Append(',');
        }

        builder.Append('|');
        builder.Append(request.MinPrice?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(request.MaxPrice?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty).Append('|');
        builder.Append(request.InStock?.ToString() ?? string.Empty).Append('|');
        builder.Append(request.Sort?.Trim().ToLowerInvariant() ?? string.Empty).Append('|');
        builder.Append(request.Page.ToString(CultureInfo.InvariantCulture)).Append('|');
        builder.Append(request.PageSize.ToString(CultureInfo.InvariantCulture));

        return $"storefront:search:{Hash(builder.ToString())}";
    }

    public static string Suggest(string query, int limit)
    {
        var normalized = $"{query.Trim().ToLowerInvariant()}|{limit.ToString(CultureInfo.InvariantCulture)}";
        return $"search:suggest:{Hash(normalized)}";
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}