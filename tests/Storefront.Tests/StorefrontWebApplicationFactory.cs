using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;

namespace Storefront.Tests;

public sealed class StorefrontWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Api:BaseUrl", "http://localhost:8080"),
                new KeyValuePair<string, string?>("Cms:BaseUrl", "http://localhost:8055"),
                new KeyValuePair<string, string?>("Cms:ApiToken", "test-token"),
                new KeyValuePair<string, string?>("Cms:CacheSeconds", "60"),
                new KeyValuePair<string, string?>("Site:BaseUrl", "https://shop.example.com"),
                new KeyValuePair<string, string?>("ConnectionStrings:Redis", string.Empty),
            ]);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStoreApiClient>();
            services.RemoveAll<IContentClient>();
            services.AddSingleton<IStoreApiClient, FakeStoreApiClient>();
            services.AddHttpClient<IContentClient, DirectusContentClient>(client =>
                {
                    client.BaseAddress = new Uri("http://localhost:8055");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCmsHttpMessageHandler());
        });
    }

    private sealed class FakeStoreApiClient : IStoreApiClient
    {
        private static readonly IReadOnlyCollection<StoreProduct> Products = BuildProducts();
        private static readonly List<StoreRedirectRule> RedirectRules = [];

        public Task<bool> AddItemToCartAsync(
            string customerId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(Guid.Parse("7e840d4c-4994-4993-b344-e8219be85656"));
        }

        public Task<StoreRedirectMatch?> ResolveRedirectAsync(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreRedirectMatch?>(null);
        }

        public Task<StoreRedirectRulePage> GetRedirectRulesAsync(
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;

            var items = RedirectRules
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArray();

            return Task.FromResult(new StoreRedirectRulePage(
                normalizedPage,
                normalizedPageSize,
                RedirectRules.Count,
                items));
        }

        public Task<Guid?> CreateRedirectRuleAsync(
            string fromPath,
            string toPath,
            int statusCode,
            CancellationToken cancellationToken)
        {
            var redirectRule = new StoreRedirectRule(
                Guid.NewGuid(),
                fromPath,
                toPath,
                statusCode,
                true,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow,
                null);

            RedirectRules.Add(redirectRule);
            return Task.FromResult<Guid?>(redirectRule.Id);
        }

        public Task<bool> DeactivateRedirectRuleAsync(Guid redirectRuleId, CancellationToken cancellationToken)
        {
            var existingRuleIndex = RedirectRules.FindIndex(rule => rule.Id == redirectRuleId);
            if (existingRuleIndex < 0)
            {
                return Task.FromResult(false);
            }

            var existingRule = RedirectRules[existingRuleIndex];
            RedirectRules[existingRuleIndex] = existingRule with
            {
                IsActive = false,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreCart?>(new StoreCart(Guid.NewGuid(), customerId, []));
        }

        public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var product = Products.SingleOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal));
            return Task.FromResult(product);
        }

        public Task<IReadOnlyCollection<StoreProduct>> GetProductsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StoreProduct>>(Products);
        }

        public Task<bool> RemoveCartItemAsync(string customerId, Guid productId, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateCartItemQuantityAsync(
            string customerId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        private static IReadOnlyCollection<StoreProduct> BuildProducts()
        {
            var products = new List<StoreProduct>
            {
                new(
                    Guid.Parse("6d4bf032-1b4f-4daa-8902-90f268cb378b"),
                    "mechanical-keyboard",
                    "Mechanical Keyboard",
                    "RGB mechanical keyboard for gamers.",
                    "Contoso",
                    "KEY-0001",
                    "/images/mechanical-keyboard.png",
                    true,
                    "keyboards",
                    "Keyboards",
                    "EUR",
                    89.00m,
                    true),
            };

            for (var index = 2; index <= 56; index++)
            {
                products.Add(
                    new StoreProduct(
                        Guid.NewGuid(),
                        $"keyboard-{index}",
                        $"Keyboard {index}",
                        $"Keyboard model {index} with tactile switches.",
                        "Contoso",
                        $"KEY-{index:0000}",
                        $"/images/keyboard-{index}.png",
                        true,
                        "keyboards",
                        "Keyboards",
                        "EUR",
                        70 + index,
                        true));
            }

            products.Add(
                new StoreProduct(
                    Guid.NewGuid(),
                    "wireless-mouse",
                    "Wireless Mouse",
                    "Compact wireless mouse.",
                    "Fabrikam",
                    "MOU-0001",
                    "/images/wireless-mouse.png",
                    true,
                    "mice",
                    "Mice",
                    "EUR",
                    39.00m,
                    true));

            return products;
        }
    }

    private sealed class FakeCmsHttpMessageHandler : HttpMessageHandler
    {
        private static readonly DateTimeOffset PublishedAt = DateTimeOffset.UtcNow.AddDays(-5);
        private static readonly DateTimeOffset UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3);

        private static readonly IReadOnlyCollection<CmsBlogPost> BlogPosts =
        [
            new CmsBlogPost(
                "published",
                "Shipping Checklist for 2026",
                "shipping-checklist-2026",
                "Practical shipping checklist to reduce cart abandonment and improve delivery reliability.",
                "Use this checklist before every campaign:\n1. Verify carrier cut-off.\n2. Update ETAs.\n3. Prepare fallback carrier options.",
                "/images/blog/shipping-checklist.jpg",
                "Alex Mercer",
                PublishedAt,
                UpdatedAt,
                ["shipping", "operations"],
                "Shipping Checklist for 2026",
                "Practical shipping checklist for modern e-commerce teams.",
                null,
                false),
            new CmsBlogPost(
                "published",
                "Private SEO Note",
                "private-seo-note",
                "Internal SEO article that is noindex.",
                "This post is published but should not appear in sitemap.",
                null,
                "Editorial Team",
                PublishedAt,
                UpdatedAt,
                ["seo"],
                null,
                null,
                null,
                true),
        ];

        private static readonly IReadOnlyCollection<CmsPage> Pages =
        [
            new CmsPage(
                "published",
                "Wholesale Program",
                "wholesale-program",
                "Partner with us and unlock volume pricing for your retail chain.",
                UpdatedAt,
                "Wholesale Program",
                "Volume pricing and dedicated support for wholesale partners.",
                null,
                false),
        ];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri;
            if (uri is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
            }

            var path = uri.AbsolutePath.TrimEnd('/');
            var query = QueryHelpers.ParseQuery(uri.Query);

            if (path.EndsWith("/items/blog_posts", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(this.BuildBlogResponse(query));
            }

            if (path.EndsWith("/items/pages", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(this.BuildPageResponse(query));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private HttpResponseMessage BuildBlogResponse(IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
        {
            var items = BlogPosts.AsEnumerable();

            if (query.TryGetValue("filter[status][_eq]", out var status))
            {
                items = items.Where(item => string.Equals(item.Status, status.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (query.TryGetValue("filter[slug][_eq]", out var slug))
            {
                items = items.Where(item => string.Equals(item.Slug, slug.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (query.TryGetValue("filter[published_at][_nnull]", out var publishedNotNull) &&
                string.Equals(publishedNotNull.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(item => item.PublishedAt is not null);
            }

            if (query.TryGetValue("filter[no_index][_eq]", out var noIndexFilter) &&
                bool.TryParse(noIndexFilter.ToString(), out var noIndex))
            {
                items = items.Where(item => item.NoIndex == noIndex);
            }

            var pagedItems = this.ApplyPaging(items, query).ToList();
            return this.BuildJsonResponse(new DirectusEnvelope<IEnumerable<CmsBlogPost>>(pagedItems));
        }

        private HttpResponseMessage BuildPageResponse(IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
        {
            var items = Pages.AsEnumerable();

            if (query.TryGetValue("filter[status][_eq]", out var status))
            {
                items = items.Where(item => string.Equals(item.Status, status.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (query.TryGetValue("filter[slug][_eq]", out var slug))
            {
                items = items.Where(item => string.Equals(item.Slug, slug.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (query.TryGetValue("filter[no_index][_eq]", out var noIndexFilter) &&
                bool.TryParse(noIndexFilter.ToString(), out var noIndex))
            {
                items = items.Where(item => item.NoIndex == noIndex);
            }

            var pagedItems = this.ApplyPaging(items, query).ToList();
            return this.BuildJsonResponse(new DirectusEnvelope<IEnumerable<CmsPage>>(pagedItems));
        }

        private IEnumerable<T> ApplyPaging<T>(
            IEnumerable<T> source,
            IReadOnlyDictionary<string, Microsoft.Extensions.Primitives.StringValues> query)
        {
            if (query.TryGetValue("limit", out var limitValue) &&
                int.TryParse(limitValue.ToString(), out var limit) &&
                limit > 0)
            {
                var page = 1;
                if (query.TryGetValue("page", out var pageValue) &&
                    int.TryParse(pageValue.ToString(), out var parsedPage) &&
                    parsedPage > 0)
                {
                    page = parsedPage;
                }

                return source.Skip((page - 1) * limit).Take(limit);
            }

            return source;
        }

        private HttpResponseMessage BuildJsonResponse<T>(T payload)
        {
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }

        private sealed record DirectusEnvelope<T>([property: JsonPropertyName("data")] T Data);

        private sealed record CmsBlogPost(
            [property: JsonPropertyName("status")] string Status,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("slug")] string Slug,
            [property: JsonPropertyName("excerpt")] string Excerpt,
            [property: JsonPropertyName("content")] string Content,
            [property: JsonPropertyName("cover_image_url")] string? CoverImageUrl,
            [property: JsonPropertyName("author_name")] string AuthorName,
            [property: JsonPropertyName("published_at")] DateTimeOffset? PublishedAt,
            [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt,
            [property: JsonPropertyName("tags")] IReadOnlyCollection<string> Tags,
            [property: JsonPropertyName("seo_title")] string? SeoTitle,
            [property: JsonPropertyName("seo_description")] string? SeoDescription,
            [property: JsonPropertyName("canonical_url")] string? CanonicalUrl,
            [property: JsonPropertyName("no_index")] bool NoIndex);

        private sealed record CmsPage(
            [property: JsonPropertyName("status")] string Status,
            [property: JsonPropertyName("title")] string Title,
            [property: JsonPropertyName("slug")] string Slug,
            [property: JsonPropertyName("content")] string Content,
            [property: JsonPropertyName("updated_at")] DateTimeOffset? UpdatedAt,
            [property: JsonPropertyName("seo_title")] string? SeoTitle,
            [property: JsonPropertyName("seo_description")] string? SeoDescription,
            [property: JsonPropertyName("canonical_url")] string? CanonicalUrl,
            [property: JsonPropertyName("no_index")] bool NoIndex);
    }
}
