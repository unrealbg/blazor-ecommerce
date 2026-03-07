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
using Microsoft.Extensions.Caching.Distributed;
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
            services.RemoveAll<DirectusContentClient>();
            services.RemoveAll<IMediaSourceFetcher>();
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();
            services.AddSingleton<IStoreApiClient, FakeStoreApiClient>();
            services.AddHttpClient<DirectusContentClient>(client =>
                {
                    client.BaseAddress = new Uri("http://localhost:8055");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new FakeCmsHttpMessageHandler());
            services.AddScoped<IContentClient>(serviceProvider =>
                ActivatorUtilities.CreateInstance<FeatureFlaggedContentClient>(serviceProvider));
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
                5.99m,
                "EUR",
                "standard",
                "Standard Delivery",
                94.99m,
                "Placed",
                "Unfulfilled",
                DateTime.UtcNow.AddDays(-1),
                new StoreOrderAddress("Alex", "Mercer", "Ship Street", "Sofia", "1000", "BG", "+359888000000"),
                new StoreOrderAddress("Alex", "Mercer", "Bill Street", "Sofia", "1000", "BG", "+359888000000"),
                [new StoreOrderLine(Guid.NewGuid(), "Mechanical Keyboard", "EUR", 89m, 1)]),
        ];

        private static readonly List<StoreShippingMethod> ShippingMethods =
        [
            new StoreShippingMethod(
                Guid.Parse("4bf25888-5d1c-40ba-a95a-f66c1f5f0b41"),
                "standard",
                "Standard Delivery",
                "Delivery in 2-4 business days.",
                "DemoCarrier",
                "Delivery",
                5.99m,
                "EUR",
                true,
                true,
                false,
                2,
                4,
                10,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-1)),
            new StoreShippingMethod(
                Guid.Parse("23f5de32-e8ec-4d4f-b317-4ffe34cc04b6"),
                "express",
                "Express Delivery",
                "Next day delivery.",
                "DemoCarrier",
                "Delivery",
                12.99m,
                "EUR",
                true,
                true,
                false,
                1,
                2,
                5,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-1)),
        ];

        private static readonly List<StoreShippingZone> ShippingZones =
        [
            new StoreShippingZone(
                Guid.Parse("0d31873c-3a23-43db-becf-0bb5cae95e8c"),
                "eu",
                "Europe",
                ["BG", "DE", "FR"],
                true,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-2)),
        ];

        private static readonly List<StoreShippingRateRule> ShippingRateRules =
        [
            new StoreShippingRateRule(
                Guid.Parse("de70af84-4e01-4f16-91ad-a3ad30270289"),
                ShippingMethods[0].Id,
                ShippingZones[0].Id,
                MinOrderAmount: null,
                MaxOrderAmount: null,
                MinWeightKg: null,
                MaxWeightKg: null,
                PriceAmount: 5.99m,
                FreeShippingThresholdAmount: 100m,
                Currency: "EUR",
                IsActive: true,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-2)),
            new StoreShippingRateRule(
                Guid.Parse("7106bc3e-5ac0-4f0b-b9bc-6b689bbddc84"),
                ShippingMethods[1].Id,
                ShippingZones[0].Id,
                MinOrderAmount: null,
                MaxOrderAmount: null,
                MinWeightKg: null,
                MaxWeightKg: null,
                PriceAmount: 12.99m,
                FreeShippingThresholdAmount: null,
                Currency: "EUR",
                IsActive: true,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-2)),
        ];

        private static readonly List<StoreShipment> Shipments = [];
        private static readonly List<StorePriceList> PriceLists =
        [
            new(
                Guid.Parse("c013cb43-20bb-4189-b0eb-75d948f1ab0a"),
                "Default EUR",
                "default",
                "EUR",
                true,
                true,
                100,
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(-1)),
        ];

        private static readonly List<StoreVariantPrice> VariantPrices = [];
        private static readonly List<StorePromotion> Promotions = [];
        private static readonly List<StoreCoupon> Coupons = [];
        private static readonly List<StoreProductReview> Reviews = BuildReviews();
        private static readonly List<StoreProductQuestion> Questions = BuildQuestions();
        private static readonly List<StoreReviewReport> ReviewReports = [];
        private static readonly List<ReviewVoteState> ReviewVotes = [];

        private static StoreCustomerProfile? currentCustomer;

        public Task<bool> AddItemToCartAsync(
            string customerId,
            Guid productId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> AddItemToCartAsync(
            string customerId,
            Guid productId,
            Guid variantId,
            int quantity,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<Guid?> CheckoutAsync(string customerId, string idempotencyKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(Guid.Parse("7e840d4c-4994-4993-b344-e8219be85656"));
        }

        public Task<bool> ApplyCouponAsync(
            string customerId,
            string couponCode,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RemoveCouponAsync(
            string customerId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
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

        public Task<StoreOrderSummary?> GetOrderAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreOrderSummary?>(Orders.SingleOrDefault(order => order.Id == orderId));
        }

        public Task<StorePaymentIntentAction?> CreatePaymentIntentAsync(
            Guid orderId,
            string? provider,
            string idempotencyKey,
            string? customerEmail,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StorePaymentIntentAction?>(new StorePaymentIntentAction(
                Guid.NewGuid(),
                provider ?? "Demo",
                "Captured",
                null,
                false,
                null));
        }

        public Task<StorePaymentIntentAction?> ConfirmPaymentIntentAsync(
            Guid paymentIntentId,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StorePaymentIntentAction?>(new StorePaymentIntentAction(
                paymentIntentId,
                "Demo",
                "Captured",
                null,
                false,
                null));
        }

        public Task<StorePaymentIntentAction?> CancelPaymentIntentAsync(
            Guid paymentIntentId,
            string? reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StorePaymentIntentAction?>(new StorePaymentIntentAction(
                paymentIntentId,
                "Demo",
                "Cancelled",
                null,
                false,
                null));
        }

        public Task<StorePaymentIntentAction?> RefundPaymentIntentAsync(
            Guid paymentIntentId,
            decimal? amount,
            string? reason,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<StorePaymentIntentAction?>(new StorePaymentIntentAction(
                paymentIntentId,
                "Demo",
                "Refunded",
                null,
                false,
                null));
        }

        public Task<StorePaymentIntentDetails?> GetPaymentIntentAsync(
            Guid paymentIntentId,
            CancellationToken cancellationToken)
        {
            var details = BuildPaymentIntentDetails(paymentIntentId);
            return Task.FromResult<StorePaymentIntentDetails?>(details);
        }

        public Task<StorePaymentIntentDetails?> GetPaymentIntentByOrderAsync(
            Guid orderId,
            CancellationToken cancellationToken)
        {
            var order = Orders.SingleOrDefault(item => item.Id == orderId);
            if (order is null)
            {
                return Task.FromResult<StorePaymentIntentDetails?>(null);
            }

            var details = BuildPaymentIntentDetails(Guid.NewGuid(), orderId);
            return Task.FromResult<StorePaymentIntentDetails?>(details);
        }

        public Task<StorePaymentIntentPage> GetPaymentIntentsAsync(
            int page,
            int pageSize,
            string? provider,
            string? status,
            CancellationToken cancellationToken)
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;
            var items = Orders
                .Select(order => new StorePaymentIntentSummary(
                    Guid.NewGuid(),
                    order.Id,
                    provider ?? "Demo",
                    status ?? "Captured",
                    order.TotalAmount,
                    order.Currency,
                    $"demo_pi_{order.Id:N}",
                    DateTime.UtcNow.AddMinutes(-30),
                    DateTime.UtcNow.AddMinutes(-10),
                    DateTime.UtcNow.AddMinutes(-10)))
                .ToArray();

            return Task.FromResult(new StorePaymentIntentPage(
                normalizedPage,
                normalizedPageSize,
                items.Length,
                items));
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

        public Task<IReadOnlyCollection<StoreShippingQuoteMethod>> GetShippingQuotesAsync(
            string countryCode,
            decimal subtotalAmount,
            string currency,
            CancellationToken cancellationToken)
        {
            var normalizedCountryCode = string.IsNullOrWhiteSpace(countryCode)
                ? "BG"
                : countryCode.Trim().ToUpperInvariant();
            var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
                ? "EUR"
                : currency.Trim().ToUpperInvariant();

            var zone = ShippingZones.FirstOrDefault(item =>
                item.IsActive &&
                item.CountryCodes.Contains(normalizedCountryCode, StringComparer.Ordinal));
            if (zone is null)
            {
                return Task.FromResult<IReadOnlyCollection<StoreShippingQuoteMethod>>([]);
            }

            var quotes = ShippingMethods
                .Where(method => method.IsActive && string.Equals(method.Currency, normalizedCurrency, StringComparison.Ordinal))
                .Select(method =>
                {
                    var rule = ShippingRateRules
                        .Where(item => item.IsActive &&
                                       item.ShippingMethodId == method.Id &&
                                       item.ShippingZoneId == zone.Id)
                        .OrderBy(item => item.PriceAmount)
                        .FirstOrDefault();

                    var price = rule?.PriceAmount ?? method.BasePriceAmount;
                    var freeThreshold = rule?.FreeShippingThresholdAmount;
                    if (freeThreshold is not null && subtotalAmount >= freeThreshold.Value)
                    {
                        price = 0m;
                    }

                    return new StoreShippingQuoteMethod(
                        method.Id,
                        method.Code,
                        method.Name,
                        method.Description,
                        price,
                        method.Currency,
                        method.EstimatedMinDays,
                        method.EstimatedMaxDays,
                        IsFreeShipping: price == 0m);
                })
                .OrderBy(quote => ShippingMethods.Single(method => method.Id == quote.Id).Priority)
                .ThenBy(quote => quote.PriceAmount)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreShippingQuoteMethod>>(quotes);
        }

        public Task<IReadOnlyCollection<StoreShippingMethod>> GetShippingMethodsAsync(
            bool activeOnly,
            CancellationToken cancellationToken)
        {
            var methods = activeOnly
                ? ShippingMethods.Where(method => method.IsActive).ToArray()
                : ShippingMethods.ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreShippingMethod>>(methods);
        }

        public Task<IReadOnlyCollection<StoreShippingZone>> GetShippingZonesAsync(
            bool activeOnly,
            CancellationToken cancellationToken)
        {
            var zones = activeOnly
                ? ShippingZones.Where(zone => zone.IsActive).ToArray()
                : ShippingZones.ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreShippingZone>>(zones);
        }

        public Task<IReadOnlyCollection<StoreShippingRateRule>> GetShippingRateRulesAsync(
            bool activeOnly,
            CancellationToken cancellationToken)
        {
            var rules = activeOnly
                ? ShippingRateRules.Where(rule => rule.IsActive).ToArray()
                : ShippingRateRules.ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreShippingRateRule>>(rules);
        }

        public Task<Guid?> CreateShippingMethodAsync(StoreShippingMethod request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            ShippingMethods.Add(request with
            {
                Id = id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            });

            return Task.FromResult<Guid?>(id);
        }

        public Task<bool> UpdateShippingMethodAsync(
            Guid shippingMethodId,
            StoreShippingMethod request,
            CancellationToken cancellationToken)
        {
            var index = ShippingMethods.FindIndex(method => method.Id == shippingMethodId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            ShippingMethods[index] = request with
            {
                Id = shippingMethodId,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<Guid?> CreateShippingZoneAsync(StoreShippingZone request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            ShippingZones.Add(request with
            {
                Id = id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            });

            return Task.FromResult<Guid?>(id);
        }

        public Task<bool> UpdateShippingZoneAsync(
            Guid shippingZoneId,
            StoreShippingZone request,
            CancellationToken cancellationToken)
        {
            var index = ShippingZones.FindIndex(zone => zone.Id == shippingZoneId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            ShippingZones[index] = request with
            {
                Id = shippingZoneId,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<Guid?> CreateShippingRateRuleAsync(StoreShippingRateRule request, CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid();
            ShippingRateRules.Add(request with
            {
                Id = id,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            });

            return Task.FromResult<Guid?>(id);
        }

        public Task<bool> UpdateShippingRateRuleAsync(
            Guid shippingRateRuleId,
            StoreShippingRateRule request,
            CancellationToken cancellationToken)
        {
            var index = ShippingRateRules.FindIndex(rule => rule.Id == shippingRateRuleId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            ShippingRateRules[index] = request with
            {
                Id = shippingRateRuleId,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<StoreShipmentPage> GetShipmentsAsync(
            string? status,
            Guid? orderId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var normalizedPage = page <= 0 ? 1 : page;
            var normalizedPageSize = pageSize <= 0 ? 20 : pageSize;

            var filtered = Shipments
                .Where(shipment => orderId is null || shipment.OrderId == orderId.Value)
                .Where(shipment => string.IsNullOrWhiteSpace(status) ||
                                   string.Equals(shipment.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var paged = filtered
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArray();

            return Task.FromResult(new StoreShipmentPage(normalizedPage, normalizedPageSize, filtered.Length, paged));
        }

        public Task<StoreShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreShipment?>(Shipments.SingleOrDefault(shipment => shipment.Id == shipmentId));
        }

        public Task<StoreShipment?> GetShipmentByOrderAsync(Guid orderId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreShipment?>(Shipments.SingleOrDefault(shipment => shipment.OrderId == orderId));
        }

        public Task<Guid?> CreateShipmentAsync(
            Guid orderId,
            string? shippingMethodCode,
            CancellationToken cancellationToken)
        {
            var order = Orders.SingleOrDefault(item => item.Id == orderId);
            var method = ShippingMethods.FirstOrDefault(item =>
                string.Equals(item.Code, shippingMethodCode ?? order?.ShippingMethodCode, StringComparison.OrdinalIgnoreCase))
                ?? ShippingMethods[0];

            var shipment = new StoreShipment(
                Guid.NewGuid(),
                orderId,
                method.Id,
                method.Provider,
                method.Code,
                $"DEMO-{orderId.ToString("N")[..8].ToUpperInvariant()}",
                $"https://shop.example.com/demo-tracking/{orderId:N}",
                "Pending",
                order is null ? "Test Recipient" : $"{order.ShippingAddress.FirstName} {order.ShippingAddress.LastName}",
                order?.ShippingAddress.Phone,
                order is null ? "{}" : JsonSerializer.Serialize(order.ShippingAddress),
                order?.ShippingPriceAmount ?? method.BasePriceAmount,
                order?.ShippingCurrency ?? method.Currency,
                null,
                null,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [
                    new StoreShipmentEvent(
                        Guid.NewGuid(),
                        "StatusChanged",
                        "Shipment created",
                        null,
                        DateTime.UtcNow,
                        null),
                ]);

            Shipments.Add(shipment);
            return Task.FromResult<Guid?>(shipment.Id);
        }

        public Task<bool> CreateShipmentLabelAsync(Guid shipmentId, CancellationToken cancellationToken)
        {
            return UpdateShipmentStatusAsync(shipmentId, "LabelCreated", "Shipment label created");
        }

        public Task<bool> MarkShipmentShippedAsync(Guid shipmentId, CancellationToken cancellationToken)
        {
            return UpdateShipmentStatusAsync(shipmentId, "Shipped", "Shipment marked as shipped");
        }

        public Task<bool> CancelShipmentAsync(
            Guid shipmentId,
            string? reason,
            CancellationToken cancellationToken)
        {
            return UpdateShipmentStatusAsync(shipmentId, "Cancelled", reason ?? "Shipment cancelled");
        }

        public Task<IReadOnlyCollection<StorePriceList>> GetPriceListsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StorePriceList>>(PriceLists.ToArray());
        }

        public Task<Guid?> CreatePriceListAsync(StorePriceListRequest request, CancellationToken cancellationToken)
        {
            var priceList = new StorePriceList(
                Guid.NewGuid(),
                request.Name,
                request.Code,
                request.Currency,
                request.IsDefault,
                request.IsActive,
                request.Priority,
                DateTime.UtcNow,
                DateTime.UtcNow);

            PriceLists.Add(priceList);
            return Task.FromResult<Guid?>(priceList.Id);
        }

        public Task<bool> UpdatePriceListAsync(
            Guid priceListId,
            StorePriceListRequest request,
            CancellationToken cancellationToken)
        {
            var index = PriceLists.FindIndex(item => item.Id == priceListId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            PriceLists[index] = PriceLists[index] with
            {
                Name = request.Name,
                Currency = request.Currency,
                IsDefault = request.IsDefault,
                IsActive = request.IsActive,
                Priority = request.Priority,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<StoreVariantPrice?> GetVariantPriceAsync(Guid variantId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StoreVariantPrice?>(VariantPrices.SingleOrDefault(item => item.VariantId == variantId));
        }

        public Task<Guid?> CreateVariantPriceAsync(StoreVariantPriceRequest request, CancellationToken cancellationToken)
        {
            var variantPrice = new StoreVariantPrice(
                Guid.NewGuid(),
                request.PriceListId,
                request.VariantId,
                request.BasePriceAmount,
                request.CompareAtPriceAmount,
                request.Currency,
                request.IsActive,
                request.ValidFromUtc,
                request.ValidToUtc,
                DateTime.UtcNow,
                DateTime.UtcNow);

            VariantPrices.RemoveAll(item => item.VariantId == request.VariantId && item.PriceListId == request.PriceListId);
            VariantPrices.Add(variantPrice);
            return Task.FromResult<Guid?>(variantPrice.Id);
        }

        public Task<bool> UpdateVariantPriceAsync(
            Guid variantPriceId,
            StoreVariantPriceRequest request,
            CancellationToken cancellationToken)
        {
            var index = VariantPrices.FindIndex(item => item.Id == variantPriceId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            VariantPrices[index] = VariantPrices[index] with
            {
                PriceListId = request.PriceListId,
                VariantId = request.VariantId,
                BasePriceAmount = request.BasePriceAmount,
                CompareAtPriceAmount = request.CompareAtPriceAmount,
                Currency = request.Currency,
                IsActive = request.IsActive,
                ValidFromUtc = request.ValidFromUtc,
                ValidToUtc = request.ValidToUtc,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<IReadOnlyCollection<StorePromotion>> GetPromotionsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StorePromotion>>(Promotions.ToArray());
        }

        public Task<StorePromotion?> GetPromotionAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            return Task.FromResult<StorePromotion?>(Promotions.SingleOrDefault(item => item.Id == promotionId));
        }

        public Task<Guid?> CreatePromotionAsync(StorePromotionRequest request, CancellationToken cancellationToken)
        {
            var promotion = new StorePromotion(
                Guid.NewGuid(),
                request.Name,
                request.Code,
                request.Type,
                0,
                request.Description,
                request.Priority,
                request.IsExclusive,
                request.AllowWithCoupons,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer,
                0,
                request.Scopes,
                request.Conditions,
                request.Benefits,
                DateTime.UtcNow,
                DateTime.UtcNow);

            Promotions.Add(promotion);
            return Task.FromResult<Guid?>(promotion.Id);
        }

        public Task<bool> UpdatePromotionAsync(
            Guid promotionId,
            StorePromotionRequest request,
            CancellationToken cancellationToken)
        {
            var index = Promotions.FindIndex(item => item.Id == promotionId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Promotions[index] = Promotions[index] with
            {
                Name = request.Name,
                Code = request.Code,
                Type = request.Type,
                Description = request.Description,
                Priority = request.Priority,
                IsExclusive = request.IsExclusive,
                AllowWithCoupons = request.AllowWithCoupons,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                UsageLimitTotal = request.UsageLimitTotal,
                UsageLimitPerCustomer = request.UsageLimitPerCustomer,
                Scopes = request.Scopes,
                Conditions = request.Conditions,
                Benefits = request.Benefits,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<bool> ActivatePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            return UpdatePromotionStatusAsync(promotionId, 1);
        }

        public Task<bool> ArchivePromotionAsync(Guid promotionId, CancellationToken cancellationToken)
        {
            return UpdatePromotionStatusAsync(promotionId, 2);
        }

        public Task<IReadOnlyCollection<StoreCoupon>> GetCouponsAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<StoreCoupon>>(Coupons.ToArray());
        }

        public Task<Guid?> CreateCouponAsync(StoreCouponRequest request, CancellationToken cancellationToken)
        {
            var coupon = new StoreCoupon(
                Guid.NewGuid(),
                request.Code,
                request.Description,
                request.PromotionId,
                0,
                request.StartAtUtc,
                request.EndAtUtc,
                request.UsageLimitTotal,
                request.UsageLimitPerCustomer,
                0,
                DateTime.UtcNow,
                DateTime.UtcNow);

            Coupons.Add(coupon);
            return Task.FromResult<Guid?>(coupon.Id);
        }

        public Task<bool> UpdateCouponAsync(
            Guid couponId,
            StoreCouponRequest request,
            CancellationToken cancellationToken)
        {
            var index = Coupons.FindIndex(item => item.Id == couponId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Coupons[index] = Coupons[index] with
            {
                Description = request.Description,
                StartAtUtc = request.StartAtUtc,
                EndAtUtc = request.EndAtUtc,
                UsageLimitTotal = request.UsageLimitTotal,
                UsageLimitPerCustomer = request.UsageLimitPerCustomer,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<bool> DisableCouponAsync(Guid couponId, CancellationToken cancellationToken)
        {
            var index = Coupons.FindIndex(item => item.Id == couponId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Coupons[index] = Coupons[index] with
            {
                Status = 1,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        public Task<StoreReviewModerationPage> GetAdminReviewsAsync(
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var filtered = Reviews
                .Where(review => string.IsNullOrWhiteSpace(status) ||
                                 string.Equals(review.Status, status, StringComparison.OrdinalIgnoreCase))
                .OrderBy(review => review.Status, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(review => review.CreatedAtUtc)
                .Select(review =>
                {
                    var product = Products.Single(item => item.Id == review.ProductId);
                    return new StoreModerationReview(
                        review.Id,
                        review.ProductId,
                        product.Name,
                        product.Slug,
                        currentCustomer?.Id ?? Guid.NewGuid(),
                        review.DisplayName,
                        review.Rating,
                        review.Title,
                        review.Body,
                        review.Status,
                        review.IsVerifiedPurchase,
                        review.ReportCount,
                        review.CreatedAtUtc);
                })
                .ToArray();

            return Task.FromResult(BuildModerationPage(filtered, page, pageSize));
        }

        public Task<bool> ApproveReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateReviewStatus(reviewId, "Approved"));
        }

        public Task<bool> RejectReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateReviewStatus(reviewId, "Rejected"));
        }

        public Task<bool> HideReviewAsync(Guid reviewId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateReviewStatus(reviewId, "Hidden"));
        }

        public Task<StoreQuestionModerationPage> GetAdminQuestionsAsync(
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var filtered = Questions
                .Where(question => string.IsNullOrWhiteSpace(status) ||
                                   string.Equals(question.Status, status, StringComparison.OrdinalIgnoreCase))
                .OrderBy(question => question.Status, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(question => question.CreatedAtUtc)
                .Select(question =>
                {
                    var product = Products.Single(item => item.Id == question.ProductId);
                    return new StoreModerationQuestion(
                        question.Id,
                        question.ProductId,
                        product.Name,
                        product.Slug,
                        question.CustomerId,
                        question.DisplayName,
                        question.QuestionText,
                        question.Status,
                        question.AnswerCount,
                        question.ReportCount,
                        question.CreatedAtUtc);
                })
                .ToArray();

            return Task.FromResult(BuildQuestionModerationPage(filtered, page, pageSize));
        }

        public Task<bool> ApproveQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateQuestionStatus(questionId, "Approved"));
        }

        public Task<bool> RejectQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateQuestionStatus(questionId, "Rejected"));
        }

        public Task<bool> HideQuestionAsync(Guid questionId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateQuestionStatus(questionId, "Hidden"));
        }

        public Task<Guid?> AddOfficialAnswerAsync(
            Guid questionId,
            string displayName,
            string answerText,
            CancellationToken cancellationToken)
        {
            var index = Questions.FindIndex(question => question.Id == questionId);
            if (index < 0)
            {
                return Task.FromResult<Guid?>(null);
            }

            var question = Questions[index];
            var answers = question.Answers.ToList();
            var answerId = Guid.NewGuid();
            answers.Add(new StoreProductAnswer(
                answerId,
                questionId,
                currentCustomer?.Id,
                displayName,
                answerText,
                "Approved",
                true,
                "Admin",
                DateTime.UtcNow));

            Questions[index] = question with
            {
                AnswerCount = answers.Count(answer => string.Equals(answer.Status, "Approved", StringComparison.OrdinalIgnoreCase)),
                Answers = answers,
            };

            return Task.FromResult<Guid?>(answerId);
        }

        public Task<StoreAnswerModerationPage> GetAdminAnswersAsync(
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var filtered = Questions
                .SelectMany(question => question.Answers.Select(answer => new { question, answer }))
                .Where(item => string.IsNullOrWhiteSpace(status) ||
                               string.Equals(item.answer.Status, status, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.answer.Status, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(item => item.answer.CreatedAtUtc)
                .Select(item =>
                {
                    var product = Products.Single(product => product.Id == item.question.ProductId);
                    return new StoreModerationAnswer(
                        item.answer.Id,
                        item.question.Id,
                        item.question.ProductId,
                        product.Name,
                        product.Slug,
                        item.question.QuestionText,
                        item.answer.CustomerId,
                        item.answer.DisplayName,
                        item.answer.AnswerText,
                        item.answer.Status,
                        item.answer.IsOfficialAnswer,
                        item.answer.AnsweredByType,
                        item.answer.CreatedAtUtc);
                })
                .ToArray();

            return Task.FromResult(BuildAnswerModerationPage(filtered, page, pageSize));
        }

        public Task<bool> ApproveAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateAnswerStatus(answerId, "Approved"));
        }

        public Task<bool> RejectAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateAnswerStatus(answerId, "Rejected"));
        }

        public Task<bool> HideAnswerAsync(Guid answerId, string? notes, CancellationToken cancellationToken)
        {
            return Task.FromResult(UpdateAnswerStatus(answerId, "Hidden"));
        }

        public Task<StoreReviewReportPage> GetReviewReportsAsync(
            string? status,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var filtered = ReviewReports
                .Where(report => string.IsNullOrWhiteSpace(status) ||
                                 string.Equals(report.Status, status, StringComparison.OrdinalIgnoreCase))
                .OrderBy(report => report.Status, StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(report => report.CreatedAtUtc)
                .ToArray();

            return Task.FromResult(BuildReviewReportPage(filtered, page, pageSize));
        }

        public Task<bool> ResolveReviewReportAsync(
            Guid reportId,
            bool dismiss,
            string? notes,
            CancellationToken cancellationToken)
        {
            var index = ReviewReports.FindIndex(report => report.Id == reportId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            ReviewReports[index] = ReviewReports[index] with
            {
                Status = dismiss ? "Dismissed" : "Resolved",
                ResolvedAtUtc = DateTime.UtcNow,
                ResolutionNotes = notes,
            };

            return Task.FromResult(true);
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

        public Task<StoreReviewSummary?> GetProductReviewSummaryAsync(Guid productId, CancellationToken cancellationToken)
        {
            var summary = BuildReviewSummary(productId);
            return Task.FromResult<StoreReviewSummary?>(summary);
        }

        public Task<StoreReviewPage> GetProductReviewsAsync(
            Guid productId,
            int page,
            int pageSize,
            string? sort,
            int? rating,
            CancellationToken cancellationToken)
        {
            var approved = Reviews
                .Where(review => review.ProductId == productId)
                .Where(review => string.Equals(review.Status, "Approved", StringComparison.OrdinalIgnoreCase));

            if (rating is >= 1 and <= 5)
            {
                approved = approved.Where(review => review.Rating == rating.Value);
            }

            approved = string.Equals(sort, "most-helpful", StringComparison.OrdinalIgnoreCase)
                ? approved.OrderByDescending(review => review.HelpfulCount).ThenByDescending(review => review.CreatedAtUtc)
                : approved.OrderByDescending(review => review.CreatedAtUtc);

            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
            var total = approved.Count();
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var items = approved
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArray();

            return Task.FromResult(new StoreReviewPage(normalizedPage, normalizedPageSize, total, totalPages, items));
        }

        public Task<StoreQuestionPage> GetProductQuestionsAsync(
            Guid productId,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var approved = Questions
                .Where(question => question.ProductId == productId)
                .Where(question => string.Equals(question.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(question => question.CreatedAtUtc)
                .ToArray();

            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 50);
            var total = approved.Length;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var items = approved
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToArray();

            return Task.FromResult(new StoreQuestionPage(normalizedPage, normalizedPageSize, total, totalPages, items));
        }

        public Task<Guid?> SubmitReviewAsync(
            Guid productId,
            StoreSubmitReviewRequest request,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<Guid?>(null);
            }

            var product = Products.SingleOrDefault(item => item.Id == productId);
            if (product is null)
            {
                return Task.FromResult<Guid?>(null);
            }

            var reviewId = Guid.NewGuid();
            var variantName = product.Variants.SingleOrDefault(variant => variant.Id == request.VariantId)?.Name;
            Reviews.Add(new StoreProductReview(
                reviewId,
                productId,
                request.VariantId,
                BuildCurrentDisplayName(),
                request.Title,
                request.Body,
                request.Rating,
                "Pending",
                false,
                null,
                DateTime.UtcNow,
                0,
                0,
                0,
                variantName));

            return Task.FromResult<Guid?>(reviewId);
        }

        public Task<bool> UpdateMyReviewAsync(
            Guid reviewId,
            StoreSubmitReviewRequest request,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult(false);
            }

            var index = Reviews.FindIndex(review => review.Id == reviewId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var review = Reviews[index];
            Reviews[index] = review with
            {
                VariantId = request.VariantId,
                Title = request.Title,
                Body = request.Body,
                Rating = request.Rating,
                Status = string.Equals(review.Status, "Approved", StringComparison.OrdinalIgnoreCase) ? "Pending" : review.Status,
            };

            return Task.FromResult(true);
        }

        public Task<StoreReviewVoteResult?> VoteReviewAsync(
            Guid reviewId,
            string voteType,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<StoreReviewVoteResult?>(null);
            }

            var index = Reviews.FindIndex(review => review.Id == reviewId);
            if (index < 0)
            {
                return Task.FromResult<StoreReviewVoteResult?>(null);
            }

            var normalizedVoteType = string.Equals(voteType, "NotHelpful", StringComparison.OrdinalIgnoreCase)
                ? "NotHelpful"
                : "Helpful";
            var voterKey = currentCustomer.Email;
            var existingVote = ReviewVotes.FindIndex(vote => vote.ReviewId == reviewId && string.Equals(vote.CustomerKey, voterKey, StringComparison.OrdinalIgnoreCase));
            var review = Reviews[index];
            var helpful = review.HelpfulCount;
            var notHelpful = review.NotHelpfulCount;

            if (existingVote >= 0)
            {
                if (string.Equals(ReviewVotes[existingVote].VoteType, normalizedVoteType, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<StoreReviewVoteResult?>(new StoreReviewVoteResult(reviewId, helpful, notHelpful, normalizedVoteType));
                }

                if (string.Equals(ReviewVotes[existingVote].VoteType, "Helpful", StringComparison.OrdinalIgnoreCase))
                {
                    helpful = Math.Max(0, helpful - 1);
                }
                else
                {
                    notHelpful = Math.Max(0, notHelpful - 1);
                }

                ReviewVotes[existingVote] = new ReviewVoteState(reviewId, voterKey, normalizedVoteType);
            }
            else
            {
                ReviewVotes.Add(new ReviewVoteState(reviewId, voterKey, normalizedVoteType));
            }

            if (string.Equals(normalizedVoteType, "Helpful", StringComparison.OrdinalIgnoreCase))
            {
                helpful++;
            }
            else
            {
                notHelpful++;
            }

            Reviews[index] = review with
            {
                HelpfulCount = helpful,
                NotHelpfulCount = notHelpful,
            };

            return Task.FromResult<StoreReviewVoteResult?>(new StoreReviewVoteResult(reviewId, helpful, notHelpful, normalizedVoteType));
        }

        public Task<Guid?> ReportReviewAsync(
            Guid reviewId,
            string reasonType,
            string? message,
            CancellationToken cancellationToken)
        {
            var reviewIndex = Reviews.FindIndex(review => review.Id == reviewId);
            if (reviewIndex < 0)
            {
                return Task.FromResult<Guid?>(null);
            }

            var review = Reviews[reviewIndex];
            var reportId = Guid.NewGuid();
            ReviewReports.Add(new StoreReviewReport(
                reportId,
                reviewId,
                review.ProductId,
                Products.Single(product => product.Id == review.ProductId).Name,
                Products.Single(product => product.Id == review.ProductId).Slug,
                currentCustomer?.Id,
                string.IsNullOrWhiteSpace(reasonType) ? "Other" : reasonType,
                message,
                "Open",
                DateTime.UtcNow,
                null,
                null));

            Reviews[reviewIndex] = review with
            {
                ReportCount = review.ReportCount + 1,
            };

            return Task.FromResult<Guid?>(reportId);
        }

        public Task<Guid?> SubmitQuestionAsync(
            Guid productId,
            StoreSubmitQuestionRequest request,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<Guid?>(null);
            }

            var questionId = Guid.NewGuid();
            Questions.Add(new StoreProductQuestion(
                questionId,
                productId,
                currentCustomer.Id,
                BuildCurrentDisplayName(),
                request.QuestionText,
                "Pending",
                DateTime.UtcNow,
                0,
                0,
                []));

            return Task.FromResult<Guid?>(questionId);
        }

        public Task<Guid?> SubmitAnswerAsync(
            Guid questionId,
            StoreSubmitAnswerRequest request,
            CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<Guid?>(null);
            }

            var index = Questions.FindIndex(question => question.Id == questionId);
            if (index < 0)
            {
                return Task.FromResult<Guid?>(null);
            }

            var answerId = Guid.NewGuid();
            var question = Questions[index];
            var answers = question.Answers.ToList();
            answers.Add(new StoreProductAnswer(
                answerId,
                questionId,
                currentCustomer.Id,
                BuildCurrentDisplayName(),
                request.AnswerText,
                "Pending",
                false,
                "Customer",
                DateTime.UtcNow));

            Questions[index] = question with
            {
                AnswerCount = answers.Count(answer => string.Equals(answer.Status, "Approved", StringComparison.OrdinalIgnoreCase)),
                Answers = answers,
            };

            return Task.FromResult<Guid?>(answerId);
        }

        public Task<IReadOnlyCollection<StoreMyReview>> GetMyReviewsAsync(CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<IReadOnlyCollection<StoreMyReview>>([]);
            }

            var myReviews = Reviews
                .Where(review => string.Equals(review.DisplayName, BuildCurrentDisplayName(), StringComparison.Ordinal))
                .Select(review =>
                {
                    var product = Products.Single(item => item.Id == review.ProductId);
                    return new StoreMyReview(
                        review.Id,
                        review.ProductId,
                        review.VariantId,
                        product.Name,
                        product.Slug,
                        review.DisplayName,
                        review.Title,
                        review.Body,
                        review.Rating,
                        review.Status,
                        review.IsVerifiedPurchase,
                        review.CreatedAtUtc);
                })
                .OrderByDescending(review => review.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreMyReview>>(myReviews);
        }

        public Task<IReadOnlyCollection<StoreMyQuestion>> GetMyQuestionsAsync(CancellationToken cancellationToken)
        {
            if (currentCustomer is null)
            {
                return Task.FromResult<IReadOnlyCollection<StoreMyQuestion>>([]);
            }

            var myQuestions = Questions
                .Where(question => question.CustomerId == currentCustomer.Id)
                .Select(question =>
                {
                    var product = Products.Single(item => item.Id == question.ProductId);
                    return new StoreMyQuestion(
                        question.Id,
                        question.ProductId,
                        product.Name,
                        product.Slug,
                        question.QuestionText,
                        question.Status,
                        question.CreatedAtUtc,
                        question.Answers);
                })
                .OrderByDescending(question => question.CreatedAtUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyCollection<StoreMyQuestion>>(myQuestions);
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

        private static Task<bool> UpdateShipmentStatusAsync(Guid shipmentId, string status, string message)
        {
            var index = Shipments.FindIndex(shipment => shipment.Id == shipmentId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            var shipment = Shipments[index];
            var events = shipment.Events.ToList();
            events.Add(new StoreShipmentEvent(
                Guid.NewGuid(),
                "StatusChanged",
                message,
                null,
                DateTime.UtcNow,
                null));

            Shipments[index] = shipment with
            {
                Status = status,
                UpdatedAtUtc = DateTime.UtcNow,
                Events = events,
            };

            return Task.FromResult(true);
        }

        private static Task<bool> UpdatePromotionStatusAsync(Guid promotionId, int status)
        {
            var index = Promotions.FindIndex(promotion => promotion.Id == promotionId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            Promotions[index] = Promotions[index] with
            {
                Status = status,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            return Task.FromResult(true);
        }

        private static StoreReviewSummary BuildReviewSummary(Guid productId)
        {
            var approved = Reviews
                .Where(review => review.ProductId == productId)
                .Where(review => string.Equals(review.Status, "Approved", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            var averageRating = approved.Length == 0
                ? 0m
                : decimal.Round(approved.Average(review => (decimal)review.Rating), 2, MidpointRounding.AwayFromZero);

            return new StoreReviewSummary(
                productId,
                approved.Length,
                averageRating,
                approved.Count(review => review.Rating == 5),
                approved.Count(review => review.Rating == 4),
                approved.Count(review => review.Rating == 3),
                approved.Count(review => review.Rating == 2),
                approved.Count(review => review.Rating == 1),
                DateTime.UtcNow);
        }

        private static bool UpdateReviewStatus(Guid reviewId, string status)
        {
            var index = Reviews.FindIndex(review => review.Id == reviewId);
            if (index < 0)
            {
                return false;
            }

            Reviews[index] = Reviews[index] with
            {
                Status = status,
            };
            return true;
        }

        private static bool UpdateQuestionStatus(Guid questionId, string status)
        {
            var index = Questions.FindIndex(question => question.Id == questionId);
            if (index < 0)
            {
                return false;
            }

            Questions[index] = Questions[index] with
            {
                Status = status,
            };
            return true;
        }

        private static bool UpdateAnswerStatus(Guid answerId, string status)
        {
            var questionIndex = Questions.FindIndex(question => question.Answers.Any(answer => answer.Id == answerId));
            if (questionIndex < 0)
            {
                return false;
            }

            var question = Questions[questionIndex];
            var answers = question.Answers.ToList();
            var answerIndex = answers.FindIndex(answer => answer.Id == answerId);
            if (answerIndex < 0)
            {
                return false;
            }

            answers[answerIndex] = answers[answerIndex] with
            {
                Status = status,
            };

            Questions[questionIndex] = question with
            {
                Answers = answers,
                AnswerCount = answers.Count(answer => string.Equals(answer.Status, "Approved", StringComparison.OrdinalIgnoreCase)),
            };

            return true;
        }

        private static StoreReviewModerationPage BuildModerationPage(
            IReadOnlyCollection<StoreModerationReview> items,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var total = items.Count;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var pagedItems = items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();
            return new StoreReviewModerationPage(normalizedPage, normalizedPageSize, total, totalPages, pagedItems);
        }

        private static StoreQuestionModerationPage BuildQuestionModerationPage(
            IReadOnlyCollection<StoreModerationQuestion> items,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var total = items.Count;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var pagedItems = items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();
            return new StoreQuestionModerationPage(normalizedPage, normalizedPageSize, total, totalPages, pagedItems);
        }

        private static StoreAnswerModerationPage BuildAnswerModerationPage(
            IReadOnlyCollection<StoreModerationAnswer> items,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var total = items.Count;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var pagedItems = items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();
            return new StoreAnswerModerationPage(normalizedPage, normalizedPageSize, total, totalPages, pagedItems);
        }

        private static StoreReviewReportPage BuildReviewReportPage(
            IReadOnlyCollection<StoreReviewReport> items,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
            var total = items.Count;
            var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)normalizedPageSize);
            var pagedItems = items.Skip((normalizedPage - 1) * normalizedPageSize).Take(normalizedPageSize).ToArray();
            return new StoreReviewReportPage(normalizedPage, normalizedPageSize, total, totalPages, pagedItems);
        }

        private static string BuildCurrentDisplayName()
        {
            if (currentCustomer is null)
            {
                return "Guest";
            }

            var fullName = string.Join(
                " ",
                new[] { currentCustomer.FirstName, currentCustomer.LastName }.Where(value => !string.IsNullOrWhiteSpace(value)));
            return string.IsNullOrWhiteSpace(fullName) ? currentCustomer.Email : fullName;
        }

        private static StorePaymentIntentDetails BuildPaymentIntentDetails(Guid paymentIntentId, Guid? orderId = null)
        {
            var fallbackOrder = Orders[0];
            var selectedOrder = orderId is null
                ? fallbackOrder
                : Orders.SingleOrDefault(order => order.Id == orderId.Value) ?? fallbackOrder;

            var customerId = Guid.TryParse(selectedOrder.CustomerId, out var parsedCustomerId)
                ? (Guid?)parsedCustomerId
                : null;

            var createdAtUtc = DateTime.UtcNow.AddMinutes(-30);
            var updatedAtUtc = DateTime.UtcNow.AddMinutes(-5);

            var transactions = new[]
            {
                new StorePaymentTransaction(
                    Guid.NewGuid(),
                    "Capture",
                    $"demo_tx_{paymentIntentId:N}",
                    selectedOrder.TotalAmount,
                    selectedOrder.Currency,
                    "Captured",
                    $"demo_pi_{selectedOrder.Id:N}",
                    updatedAtUtc,
                    null),
            };

            return new StorePaymentIntentDetails(
                paymentIntentId,
                selectedOrder.Id,
                customerId,
                "Demo",
                "Captured",
                selectedOrder.TotalAmount,
                selectedOrder.Currency,
                $"demo_pi_{selectedOrder.Id:N}",
                $"demo_cs_{paymentIntentId:N}",
                null,
                null,
                createdAtUtc,
                updatedAtUtc,
                updatedAtUtc,
                transactions);
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

        private static List<StoreProductReview> BuildReviews()
        {
            var product = Products.Single(item => string.Equals(item.Slug, "mechanical-keyboard", StringComparison.Ordinal));
            var defaultVariant = product.Variants.OrderBy(variant => variant.Position).FirstOrDefault();

            return
            [
                new StoreProductReview(
                    Guid.Parse("cb26e2d0-f217-4c67-aa98-2b22ff9044ab"),
                    product.Id,
                    defaultVariant?.Id,
                    "Jamie Carter",
                    "Excellent keyboard",
                    "Switches feel precise and the build quality is excellent.",
                    5,
                    "Approved",
                    true,
                    Guid.NewGuid(),
                    DateTime.UtcNow.AddDays(-10),
                    7,
                    1,
                    0,
                    defaultVariant?.Name),
                new StoreProductReview(
                    Guid.Parse("e169ce98-a33f-4495-b0c8-7a457a70c55b"),
                    product.Id,
                    defaultVariant?.Id,
                    "Morgan Lee",
                    "Great for work",
                    "Stable wireless connection and comfortable layout for long sessions.",
                    4,
                    "Approved",
                    false,
                    null,
                    DateTime.UtcNow.AddDays(-4),
                    3,
                    0,
                    0,
                    defaultVariant?.Name),
                new StoreProductReview(
                    Guid.Parse("feff6626-625d-47ca-a0b1-f9858354ec49"),
                    product.Id,
                    defaultVariant?.Id,
                    "Pending User",
                    "Awaiting moderation",
                    "This review should not appear publicly until approved.",
                    5,
                    "Pending",
                    false,
                    null,
                    DateTime.UtcNow.AddDays(-1),
                    0,
                    0,
                    0,
                    defaultVariant?.Name),
            ];
        }

        private static List<StoreProductQuestion> BuildQuestions()
        {
            var product = Products.Single(item => string.Equals(item.Slug, "mechanical-keyboard", StringComparison.Ordinal));
            return
            [
                new StoreProductQuestion(
                    Guid.Parse("3e56780d-8b13-4f99-b66e-f0bdc14ebfe0"),
                    product.Id,
                    Guid.NewGuid(),
                    "Taylor Reed",
                    "Does this keyboard support Mac shortcuts out of the box?",
                    "Approved",
                    DateTime.UtcNow.AddDays(-6),
                    1,
                    0,
                    [
                        new StoreProductAnswer(
                            Guid.Parse("e0b1caec-3789-4216-b3ab-e0f3ad1195e1"),
                            Guid.Parse("3e56780d-8b13-4f99-b66e-f0bdc14ebfe0"),
                            null,
                            "Support Team",
                            "Yes. It ships with Mac legends and you can switch layout profiles in firmware.",
                            "Approved",
                            true,
                            "Admin",
                            DateTime.UtcNow.AddDays(-5)),
                    ]),
            ];
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

        private sealed record ReviewVoteState(Guid ReviewId, string CustomerKey, string VoteType);
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
