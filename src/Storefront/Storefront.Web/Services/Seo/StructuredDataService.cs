using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Storefront.Web.Services.Seo;

public sealed class StructuredDataService(IPageMetadataService pageMetadataService) : IStructuredDataService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string BuildProductJsonLd(ProductSeoModel model, string canonicalUrl)
    {
        var normalizedCanonical = EnsureAbsoluteUrl(canonicalUrl);
        var normalizedImageUrls = model.ImageUrls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => NormalizeImageUrl(url, normalizedCanonical))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var normalizedDescription = NormalizeDescription(model.Description);

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = model.Name,
            ["description"] = normalizedDescription,
            ["sku"] = model.Sku,
            ["brand"] = string.IsNullOrWhiteSpace(model.Brand)
                ? null
                : new Dictionary<string, object?>
                {
                    ["@type"] = "Brand",
                    ["name"] = model.Brand,
                },
            ["image"] = normalizedImageUrls.Length == 0 ? null : normalizedImageUrls,
            ["aggregateRating"] = model.ApprovedReviewCount > 0 && model.AverageRating is > 0m
                ? new Dictionary<string, object?>
                {
                    ["@type"] = "AggregateRating",
                    ["ratingValue"] = model.AverageRating.Value.ToString("0.00", CultureInfo.InvariantCulture),
                    ["reviewCount"] = model.ApprovedReviewCount,
                }
                : null,
            ["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["url"] = normalizedCanonical,
                ["priceCurrency"] = model.Currency,
                ["price"] = model.PriceAmount.ToString("0.00", CultureInfo.InvariantCulture),
                ["availability"] = model.IsInStock
                    ? "https://schema.org/InStock"
                    : "https://schema.org/OutOfStock",
                ["itemCondition"] = "https://schema.org/NewCondition",
            },
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string BuildBlogPostingJsonLd(BlogPostingSeoModel model, string canonicalUrl)
    {
        var normalizedCanonical = EnsureAbsoluteUrl(canonicalUrl);
        var normalizedImageUrl = NormalizeImageUrl(model.ImageUrl, normalizedCanonical);
        var normalizedDescription = NormalizeDescription(model.Description);

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = model.Headline,
            ["description"] = normalizedDescription,
            ["image"] = normalizedImageUrl is null ? null : new[] { normalizedImageUrl },
            ["datePublished"] = model.DatePublished.ToString("O", CultureInfo.InvariantCulture),
            ["dateModified"] = model.DateModified.ToString("O", CultureInfo.InvariantCulture),
            ["author"] = new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["name"] = model.AuthorName,
            },
            ["mainEntityOfPage"] = new Dictionary<string, object?>
            {
                ["@type"] = "WebPage",
                ["@id"] = normalizedCanonical,
            },
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string BuildBreadcrumbJsonLd(IEnumerable<BreadcrumbItem> items)
    {
        var normalizedItems = items
            .Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name,
                ["item"] = EnsureAbsoluteUrl(item.Url),
            })
            .ToArray();

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = normalizedItems,
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    public string BuildWebSiteSearchJsonLd(string siteBaseUrl)
    {
        var normalizedBaseUrl = EnsureAbsoluteUrl(siteBaseUrl);

        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "WebSite",
            ["url"] = normalizedBaseUrl,
            ["potentialAction"] = new Dictionary<string, object?>
            {
                ["@type"] = "SearchAction",
                ["target"] = $"{normalizedBaseUrl}/search?q={{search_term_string}}",
                ["query-input"] = "required name=search_term_string",
            },
        };

        return JsonSerializer.Serialize(payload, SerializerOptions);
    }

    private string EnsureAbsoluteUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url.TrimEnd('/');
        }

        return pageMetadataService.BuildAbsoluteUrl(url).TrimEnd('/');
    }

    private string NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return "Buy this product online with fast and secure checkout.";
        }

        var trimmedDescription = description.Trim();
        return trimmedDescription.Length <= 500 ? trimmedDescription : trimmedDescription[..500];
    }

    private string? NormalizeImageUrl(string? imageUrl, string canonicalUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return imageUrl;
        }

        if (Uri.TryCreate(canonicalUrl, UriKind.Absolute, out var canonicalUri))
        {
            var baseUri = $"{canonicalUri.Scheme}://{canonicalUri.Authority}";
            return imageUrl.StartsWith("/", StringComparison.Ordinal)
                ? $"{baseUri}{imageUrl}"
                : $"{baseUri}/{imageUrl}";
        }

        return imageUrl;
    }
}
