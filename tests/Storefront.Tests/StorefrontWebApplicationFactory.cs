using System.Globalization;
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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;
using Storefront.Web.Services.Media;

namespace Storefront.Tests;

public sealed class StorefrontWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string MediaCachePath { get; } = Path.Combine(
        Path.GetTempPath(),
        $"storefront-media-cache-{Guid.NewGuid():N}");

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
                new KeyValuePair<string, string?>("Media:AllowedHosts:0", "localhost:8055"),
                new KeyValuePair<string, string?>("Media:AllowedHosts:1", "shop.example.com"),
                new KeyValuePair<string, string?>("Media:AllowedHosts:2", "localhost:5100"),
                new KeyValuePair<string, string?>("Media:CachePath", this.MediaCachePath),
                new KeyValuePair<string, string?>("Media:DefaultQualityJpeg", "82"),
                new KeyValuePair<string, string?>("Media:DefaultQualityWebp", "80"),
                new KeyValuePair<string, string?>("Media:DefaultQualityAvif", "55"),
                new KeyValuePair<string, string?>("Media:EnableAvif", "true"),
                new KeyValuePair<string, string?>("Media:MaxSourceBytes", "20971520"),
                new KeyValuePair<string, string?>("Media:FetchTimeoutSeconds", "10"),
                new KeyValuePair<string, string?>("Media:AllowUpscale", "false"),
                new KeyValuePair<string, string?>("ConnectionStrings:Redis", string.Empty),
            ]);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStoreApiClient>();
            services.RemoveAll<IContentClient>();
            services.RemoveAll<IMediaSourceFetcher>();
            services.AddSingleton<IStoreApiClient, FakeStoreApiClient>();
            services.AddHttpClient<IContentClient, DirectusContentClient>(client =>
                {
                    client.BaseAddress = new Uri("http://localhost:8055");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCmsHttpMessageHandler());
            services.AddHttpClient<IMediaSourceFetcher, MediaSourceFetcher>(client =>
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
        private static readonly List<StoreCustomerAddress> Addresses = [];
        private static readonly List<StoreOrderSummary> Orders =
        [
            new StoreOrderSummary(
                Guid.Parse("4672fbc9-c711-49a1-a4df-419f95dd70ab"),
                "sample-customer",
                "EUR",
                89m,
                89m,
                "Placed",
                DateTime.UtcNow.AddDays(-1),
                new StoreOrderAddress("Alex", "Mercer", "Ship Street", "Sofia", "1000", "BG", "+359888000000"),
                new StoreOrderAddress("Alex", "Mercer", "Bill Street", "Sofia", "1000", "BG", "+359888000000"),
                [new StoreOrderLine(Guid.NewGuid(), "Mechanical Keyboard", "EUR", 89m, 1)]),
        ];

        private static StoreCustomerProfile? currentCustomer;

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

        public Task<Guid?> CheckoutAsync(
            StoreCheckoutRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(Guid.Parse("7e840d4c-4994-4993-b344-e8219be85656"));
        }

        public Task<StoreAuthResponse?> RegisterAsync(
            string email,
            string password,
            string? firstName,
            string? lastName,
            string? phoneNumber,
            CancellationToken cancellationToken)
        {
            var auth = new StoreAuthResponse(Guid.NewGuid(), Guid.NewGuid(), email.Trim().ToLowerInvariant());
            currentCustomer = new StoreCustomerProfile(
                auth.CustomerId,
                auth.Email,
                firstName,
                lastName,
                phoneNumber,
                false,
                true,
                Addresses.ToArray());

            return Task.FromResult<StoreAuthResponse?>(auth);
        }

        public Task<StoreAuthResponse?> LoginAsync(
            string email,
            string password,
            bool rememberMe,
            CancellationToken cancellationToken)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            currentCustomer ??= new StoreCustomerProfile(
                Guid.NewGuid(),
                normalizedEmail,
                "Alex",
                "Mercer",
                "+359888000000",
                true,
                true,
                Addresses.ToArray());

            var auth = new StoreAuthResponse(Guid.NewGuid(), currentCustomer.Id, normalizedEmail);
            return Task.FromResult<StoreAuthResponse?>(auth);
        }

        public Task<bool> LogoutAsync(CancellationToken cancellationToken)
        {
            currentCustomer = null;
            return Task.FromResult(true);
        }

        public Task<bool> ForgotPasswordAsync(string email, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ResetPasswordAsync(
            string email,
            string token,
            string newPassword,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<StoreCustomerProfile?> GetCurrentCustomerAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(currentCustomer);
        }

        public Task<bool> UpdateCurrentCustomerAsync(
            StoreUpdateProfileRequest request,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult(false);
            }

            currentCustomer = currentCustomer with
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
            };

            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<StoreCustomerAddress>> GetCurrentCustomerAddressesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StoreCustomerAddress>>(Addresses.ToArray());
        }

        public Task<Guid?> AddCurrentCustomerAddressAsync(StoreAddressRequest request, CancellationToken cancellationToken)
        {
            var address = new StoreCustomerAddress(
                Guid.NewGuid(),
                request.Label,
                request.FirstName,
                request.LastName,
                request.Company,
                request.Street1,
                request.Street2,
                request.City,
                request.PostalCode,
                request.CountryCode,
                request.Phone,
                request.IsDefaultShipping,
                request.IsDefaultBilling);

            Addresses.Add(address);
            return Task.FromResult<Guid?>(address.Id);
        }

        public Task<bool> UpdateCurrentCustomerAddressAsync(
            Guid addressId,
            StoreAddressRequest request,
            CancellationToken cancellationToken)
        {
            var index = Addresses.FindIndex(address => address.Id == addressId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Addresses[index] = new StoreCustomerAddress(
                addressId,
                request.Label,
                request.FirstName,
                request.LastName,
                request.Company,
                request.Street1,
                request.Street2,
                request.City,
                request.PostalCode,
                request.CountryCode,
                request.Phone,
                request.IsDefaultShipping,
                request.IsDefaultBilling);

            return Task.FromResult(true);
        }

        public Task<bool> DeleteCurrentCustomerAddressAsync(Guid addressId, CancellationToken cancellationToken)
        {
            Addresses.RemoveAll(address => address.Id == addressId);
            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<StoreOrderSummary>> GetMyOrdersAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StoreOrderSummary>>(currentCustomer is null ? [] : Orders);
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

        public Task<StoreInventoryProductDetails?> GetInventoryProductAsync(
            Guid productId,
            CancellationToken cancellationToken)
        {
            var product = Products.SingleOrDefault(item => item.Id == productId);
            if (product is null)
            {
                return Task.FromResult<StoreInventoryProductDetails?>(null);
            }

            var availableQuantity = product.IsInStock ? 100 : 0;
            var details = new StoreInventoryProductDetails(
                new StoreStockItemSummary(
                    Guid.NewGuid(),
                    product.Id,
                    product.Sku,
                    100,
                    0,
                    availableQuantity,
                    product.IsTracked,
                    product.AllowBackorder,
                    product.IsInStock,
                    DateTime.UtcNow.AddDays(-5),
                    DateTime.UtcNow),
                0,
                0);

            return Task.FromResult<StoreInventoryProductDetails?>(details);
        }

        public Task<bool> AdjustInventoryStockAsync(
            Guid productId,
            int quantityDelta,
            string? reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(quantityDelta != 0 && Products.Any(product => product.Id == productId));
        }

        public Task<StoreStockMovementPage> GetInventoryMovementsAsync(
            Guid productId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var movements = new List<StoreStockMovement>
            {
                new(
                    Guid.NewGuid(),
                    productId,
                    null,
                    "ManualAdjustment",
                    10,
                    null,
                    "Initial stock",
                    DateTime.UtcNow.AddDays(-2),
                    "system"),
            };

            return Task.FromResult(new StoreStockMovementPage(1, pageSize <= 0 ? 50 : pageSize, movements.Count, movements));
        }

        public Task<StoreStockReservationPage> GetActiveInventoryReservationsAsync(
            Guid? productId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<StoreStockReservation> reservations = [];
            return Task.FromResult(new StoreStockReservationPage(1, pageSize <= 0 ? 50 : pageSize, reservations.Count, reservations));
        }

        public Task<StoreCart?> GetCartAsync(string customerId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreCart?>(new StoreCart(Guid.NewGuid(), customerId, [], []));
        }

        public Task<StoreProduct?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken)
        {
            var product = Products.SingleOrDefault(item => string.Equals(item.Slug, slug, StringComparison.Ordinal));
            return Task.FromResult(product);
        }

        public Task<StoreSearchProductsResponse> SearchProductsAsync(
            StoreSearchProductsRequest request,
            CancellationToken cancellationToken)
        {
            var normalizedQuery = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();
            var normalizedCategory = string.IsNullOrWhiteSpace(request.CategorySlug)
                ? null
                : request.CategorySlug.Trim().ToLowerInvariant();
            var normalizedBrands = request.Brands
                .Where(brand => !string.IsNullOrWhiteSpace(brand))
                .Select(brand => brand.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(brand => brand, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var normalizedMinPrice = request.MinPrice is > 0 ? request.MinPrice : null;
            var normalizedMaxPrice = request.MaxPrice is > 0 ? request.MaxPrice : null;
            if (normalizedMinPrice is not null && normalizedMaxPrice is not null && normalizedMinPrice > normalizedMaxPrice)
            {
                (normalizedMinPrice, normalizedMaxPrice) = (normalizedMaxPrice, normalizedMinPrice);
            }

            var normalizedSort = NormalizeSort(request.Sort, normalizedQuery);
            var normalizedPage = request.Page <= 0 ? 1 : request.Page;
            var normalizedPageSize = request.PageSize <= 0 ? 24 : Math.Min(100, request.PageSize);

            var activeProducts = Products
                .Where(product => product.IsActive)
                .Where(product => string.Equals(product.Currency, "EUR", StringComparison.Ordinal))
                .ToList();

            var queryFiltered = ApplyQuery(activeProducts, normalizedQuery);
            var stockFiltered = ApplyStock(queryFiltered, request.InStock);
            var categoryFiltered = ApplyCategory(stockFiltered, normalizedCategory);
            var brandFiltered = ApplyBrands(categoryFiltered, normalizedBrands);

            var priceSummaryScope = brandFiltered.ToList();
            var priceSummary = new StoreSearchPriceSummary(
                priceSummaryScope.Count == 0 ? null : priceSummaryScope.Min(product => product.Amount),
                priceSummaryScope.Count == 0 ? null : priceSummaryScope.Max(product => product.Amount));

            var filteredForResult = ApplyPrice(brandFiltered, normalizedMinPrice, normalizedMaxPrice).ToList();
            var total = filteredForResult.Count;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            normalizedPage = Math.Min(normalizedPage, totalPages);

            var sorted = SortProducts(filteredForResult, normalizedSort, normalizedQuery);
            var pageItems = sorted
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .Select(MapToSearchItem)
                .ToArray();

            var inStockCount = categoryFiltered.Count(product => product.IsInStock);
            var brandFacetItems = BuildBrandFacets(
                ApplyPrice(categoryFiltered, normalizedMinPrice, normalizedMaxPrice),
                normalizedBrands);
            var categoryFacetItems = BuildCategoryFacets(
                ApplyPrice(ApplyBrands(stockFiltered, normalizedBrands), normalizedMinPrice, normalizedMaxPrice),
                normalizedCategory);

            var response = new StoreSearchProductsResponse(
                pageItems,
                total,
                normalizedPage,
                normalizedPageSize,
                totalPages,
                new StoreSearchFacets(
                    brandFacetItems,
                    categoryFacetItems,
                    inStockCount,
                    priceSummary),
                new StoreSearchAppliedFilters(
                    normalizedQuery,
                    normalizedCategory,
                    normalizedBrands,
                    normalizedMinPrice,
                    normalizedMaxPrice,
                    request.InStock,
                    normalizedSort,
                    normalizedPage,
                    normalizedPageSize));

            return Task.FromResult(response);
        }

        public Task<StoreSearchSuggestionsResponse> SuggestProductsAsync(
            string query,
            int limit,
            CancellationToken cancellationToken)
        {
            var normalizedQuery = query.Trim();
            if (normalizedQuery.Length < 2)
            {
                return Task.FromResult(new StoreSearchSuggestionsResponse(normalizedQuery, []));
            }

            var normalizedLower = normalizedQuery.ToLowerInvariant();
            var normalizedLimit = Math.Clamp(limit, 1, 10);

            var suggestions = Products
                .Where(product => product.IsActive)
                .Where(product =>
                    product.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(product.Brand) &&
                     product.Brand.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(product => product.Name.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .ThenBy(product => product.Name, StringComparer.OrdinalIgnoreCase)
                .Take(normalizedLimit)
                .Select(product => new StoreSearchSuggestionItem(
                    product.Name,
                    product.Slug,
                    product.ImageUrl,
                    product.Amount,
                    product.Currency))
                .ToArray();

            return Task.FromResult(new StoreSearchSuggestionsResponse(normalizedLower, suggestions));
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

        private static IReadOnlyCollection<StoreSearchBrandFacetItem> BuildBrandFacets(
            IEnumerable<StoreProduct> products,
            IReadOnlyCollection<string> selectedBrands)
        {
            var selectedSet = selectedBrands.ToHashSet(StringComparer.OrdinalIgnoreCase);

            return products
                .Where(product => !string.IsNullOrWhiteSpace(product.Brand))
                .GroupBy(product => product.Brand!)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => new StoreSearchBrandFacetItem(
                    group.Key,
                    group.Count(),
                    selectedSet.Contains(group.Key)))
                .ToArray();
        }

        private static IReadOnlyCollection<StoreSearchCategoryFacetItem> BuildCategoryFacets(
            IEnumerable<StoreProduct> products,
            string? selectedCategory)
        {
            return products
                .Where(product => !string.IsNullOrWhiteSpace(product.CategorySlug) &&
                                  !string.IsNullOrWhiteSpace(product.CategoryName))
                .GroupBy(product => new { product.CategorySlug, product.CategoryName })
                .OrderBy(group => group.Key.CategoryName, StringComparer.OrdinalIgnoreCase)
                .Select(group => new
                {
                    group.Key.CategorySlug,
                    group.Key.CategoryName,
                    Count = group.Count(),
                    Selected = selectedCategory is not null &&
                               string.Equals(
                                   group.Key.CategorySlug,
                                   selectedCategory,
                                   StringComparison.OrdinalIgnoreCase),
                })
                .Select(item => new StoreSearchCategoryFacetItem(
                    item.CategorySlug!,
                    item.CategoryName!,
                    item.Count,
                    item.Selected))
                .ToArray();
        }

        private static IEnumerable<StoreProduct> ApplyQuery(
            IEnumerable<StoreProduct> products,
            string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return products;
            }

            return products.Where(product =>
                product.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(product.Description) &&
                 product.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(product.Brand) &&
                 product.Brand.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(product.CategoryName) &&
                 product.CategoryName.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        private static IEnumerable<StoreProduct> ApplyStock(
            IEnumerable<StoreProduct> products,
            bool? inStock)
        {
            return inStock switch
            {
                true => products.Where(product => product.IsInStock),
                false => products.Where(product => !product.IsInStock),
                _ => products,
            };
        }

        private static IEnumerable<StoreProduct> ApplyCategory(
            IEnumerable<StoreProduct> products,
            string? categorySlug)
        {
            if (string.IsNullOrWhiteSpace(categorySlug))
            {
                return products;
            }

            return products.Where(product => string.Equals(
                product.CategorySlug,
                categorySlug,
                StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<StoreProduct> ApplyBrands(
            IEnumerable<StoreProduct> products,
            IReadOnlyCollection<string> brands)
        {
            if (brands.Count == 0)
            {
                return products;
            }

            return products.Where(product =>
                product.Brand is not null &&
                brands.Contains(product.Brand, StringComparer.OrdinalIgnoreCase));
        }

        private static IEnumerable<StoreProduct> ApplyPrice(
            IEnumerable<StoreProduct> products,
            decimal? minPrice,
            decimal? maxPrice)
        {
            if (minPrice is not null)
            {
                products = products.Where(product => product.Amount >= minPrice.Value);
            }

            if (maxPrice is not null)
            {
                products = products.Where(product => product.Amount <= maxPrice.Value);
            }

            return products;
        }

        private static string NormalizeSort(string? sort, string? query)
        {
            var defaultSort = string.IsNullOrWhiteSpace(query) ? "popular" : "relevance";
            if (string.IsNullOrWhiteSpace(sort))
            {
                return defaultSort;
            }

            var normalized = sort.Trim().ToLowerInvariant();
            return normalized switch
            {
                "relevance" => "relevance",
                "popular" => "popular",
                "newest" => "newest",
                "price_asc" => "price_asc",
                "price_desc" => "price_desc",
                "name_asc" => "name_asc",
                _ => defaultSort,
            };
        }

        private static IReadOnlyCollection<StoreProduct> SortProducts(
            IReadOnlyCollection<StoreProduct> products,
            string sort,
            string? query)
        {
            return sort switch
            {
                "newest" => products.OrderByDescending(product => product.Slug, StringComparer.Ordinal).ToArray(),
                "price_asc" => products.OrderBy(product => product.Amount).ThenBy(product => product.Name).ToArray(),
                "price_desc" => products.OrderByDescending(product => product.Amount).ThenBy(product => product.Name).ToArray(),
                "name_asc" => products.OrderBy(product => product.Name).ToArray(),
                "relevance" => SortByRelevance(products, query),
                _ => products.OrderBy(product => product.Name).ToArray(),
            };
        }

        private static IReadOnlyCollection<StoreProduct> SortByRelevance(
            IReadOnlyCollection<StoreProduct> products,
            string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return products.OrderBy(product => product.Name).ToArray();
            }

            return products
                .OrderByDescending(product => string.Equals(product.Name, query, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(product => product.Name.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(product => product.IsInStock)
                .ThenBy(product => product.Name)
                .ToArray();
        }

        private static StoreSearchProductItem MapToSearchItem(StoreProduct product)
        {
            return new StoreSearchProductItem(
                product.Id,
                product.Slug,
                product.Name,
                product.Description,
                product.CategorySlug,
                product.CategoryName,
                product.Brand,
                product.Amount,
                product.Currency,
                product.IsInStock,
                product.ImageUrl,
                DateTime.UtcNow);
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
                    true,
                    false,
                    20,
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
                        true,
                        false,
                        50,
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
                    true,
                    false,
                    12,
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
        private static readonly byte[] PngImage = BuildPngImage();

        private static readonly IReadOnlyCollection<CmsBlogPost> BlogPosts =
        [
            new CmsBlogPost(
                "published",
                "Shipping Checklist for 2026",
                "shipping-checklist-2026",
                "Practical shipping checklist to reduce cart abandonment and improve delivery reliability.",
                "Use this checklist before every campaign:\n\n![Packaging Bench](http://localhost:8055/assets/blog-inline.png)\n\n1. Verify carrier cut-off.\n2. Update ETAs.\n3. Prepare fallback carrier options.",
                "http://localhost:8055/assets/blog-cover.png",
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

            if (path.Contains("/assets/", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(this.BuildAssetResponse(path));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static byte[] BuildPngImage()
        {
            using var image = new Image<Rgba32>(16, 16);
            image.Mutate(context => context.BackgroundColor(new Rgba32(15, 118, 110, 255)));

            using var output = new MemoryStream();
            image.SaveAsPng(output);
            return output.ToArray();
        }

        private HttpResponseMessage BuildAssetResponse(string path)
        {
            if (path.EndsWith("/assets/missing.png", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }

            if (path.EndsWith("/assets/blog-cover.png", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/assets/blog-inline.png", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith("/assets/test.png", StringComparison.OrdinalIgnoreCase))
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(PngImage),
                };

                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                response.Content.Headers.LastModified = UpdatedAt;
                return response;
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
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
