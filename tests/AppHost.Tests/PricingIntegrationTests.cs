using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Domain.Shared;
using Catalog.Api;
using Catalog.Infrastructure.Persistence;
using Customers.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Domain.Coupons;
using Pricing.Domain.PriceLists;
using Pricing.Domain.Promotions;
using Pricing.Domain.VariantPrices;
using Pricing.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class PricingIntegrationTests
{
    [Fact]
    public async Task VariantPricingEndpoint_ShouldReturnExplicitVariantPrice()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Variant priced product", 120m);
        await SeedVariantPriceAsync(factory, product.VariantId, 99m, 149m);

        var response = await client.GetFromJsonAsync<VariantPricingPayload>($"/api/v1/pricing/variants/{product.VariantId:D}");

        Assert.NotNull(response);
        Assert.Equal(product.VariantId, response!.VariantId);
        Assert.Equal(99m, response.EffectivePriceAmount);
        Assert.Equal(149m, response.CompareAtPriceAmount);
        Assert.True(response.IsDiscounted);
    }

    [Fact]
    public async Task Cart_ShouldReflectAutomaticProductPromotion()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Automatic promo product", 100m);
        await SeedActivePromotionAsync(
            factory,
            "Automatic Product Discount",
            [new PromotionScopeData(PromotionScopeType.Product, product.ProductId)],
            [],
            [new PromotionBenefitData(PromotionBenefitType.PercentageOff, null, 10m, null, false)]);

        var customerId = $"pricing-cart-{Guid.NewGuid():N}";
        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(product.ProductId, product.VariantId, 1));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var cart = await client.GetFromJsonAsync<CartPayload>($"/api/v1/cart/{customerId}");

        Assert.NotNull(cart);
        Assert.Equal(100m, cart!.SubtotalBeforeDiscountAmount);
        Assert.Equal(90m, cart.SubtotalAmount);
        Assert.Equal(10m, cart.LineDiscountTotalAmount);
        Assert.Equal(90m, cart.GrandTotalAmount);
        Assert.Single(cart.Lines);
        var line = Assert.Single(cart.Lines);
        Assert.Equal(90m, line.FinalUnitAmount);
        Assert.Equal(10m, line.DiscountTotalAmount);
    }

    [Fact]
    public async Task ApplyingCoupon_ShouldChangeCartTotals()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Coupon product", 100m);
        var couponCode = await SeedCouponPromotionAsync(factory, "SAVE15", 15m);
        var customerId = $"pricing-coupon-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(product.ProductId, product.VariantId, 1));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var couponResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/coupon",
            new { couponCode });
        Assert.Equal(HttpStatusCode.OK, couponResponse.StatusCode);

        var cart = await client.GetFromJsonAsync<CartPayload>($"/api/v1/cart/{customerId}");

        Assert.NotNull(cart);
        Assert.Equal(couponCode, cart!.AppliedCouponCode);
        Assert.Equal(100m, cart.SubtotalBeforeDiscountAmount);
        Assert.Equal(15m, cart.CartDiscountTotalAmount);
        Assert.Equal(85m, cart.SubtotalAmount);
        Assert.Equal(85m, cart.GrandTotalAmount);
    }

    [Fact]
    public async Task InvalidCoupon_ShouldReturnNotFound()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var customerId = $"pricing-invalid-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/coupon",
            new { couponCode = "UNKNOWN" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_ShouldSnapshotDiscountsIntoOrder()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Checkout promo product", 100m);
        var couponCode = await SeedCouponPromotionAsync(factory, "ORDER20", 20m);
        var customerId = $"pricing-checkout-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(product.ProductId, product.VariantId, 1));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var couponResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/coupon",
            new { couponCode });
        Assert.Equal(HttpStatusCode.OK, couponResponse.StatusCode);

        using var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/checkout/{customerId}");
        checkoutRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        using var checkoutResponse = await client.SendAsync(checkoutRequest);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var checkoutPayload = await checkoutResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(checkoutPayload);

        var order = await client.GetFromJsonAsync<OrderPayload>($"/api/v1/orders/{checkoutPayload!.Id:D}");

        Assert.NotNull(order);
        Assert.Equal(100m, order!.SubtotalBeforeDiscountAmount);
        Assert.Equal(20m, order.CartDiscountTotalAmount);
        Assert.Equal(80m, order.SubtotalAmount);
        Assert.Equal(80m, order.TotalAmount);
        Assert.Contains("ORDER20", order.AppliedCouponsJson, StringComparison.OrdinalIgnoreCase);
        var line = Assert.Single(order.Lines);
        Assert.Equal(product.VariantId, line.VariantId);
        Assert.Equal(100m, line.BaseUnitAmount);
        Assert.Equal(80m, line.FinalUnitAmount);
        Assert.Equal(20m, line.DiscountTotalAmount);
    }

    [Fact]
    public async Task PaymentWebhookReplay_ShouldNotDoubleCountPromotionRedemption()
    {
        await using var factory = CreateManualCaptureFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Redemption product", 100m);
        var couponCode = await SeedCouponPromotionAsync(factory, "REDEEM10", 10m);
        var customerId = $"pricing-redeem-{Guid.NewGuid():N}";

        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(product.ProductId, product.VariantId, 1));
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var couponResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/coupon",
            new { couponCode });
        Assert.Equal(HttpStatusCode.OK, couponResponse.StatusCode);

        using var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/orders/checkout/{customerId}");
        checkoutRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        using var checkoutResponse = await client.SendAsync(checkoutRequest);
        Assert.Equal(HttpStatusCode.Created, checkoutResponse.StatusCode);
        var checkoutPayload = await checkoutResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(checkoutPayload);

        var payment = await CreatePaymentIntentAsync(client, checkoutPayload!.Id, Guid.NewGuid().ToString("N"));
        Assert.Equal("Pending", payment.Status);

        var paymentDetails = await client.GetFromJsonAsync<PaymentIntentDetailsPayload>($"/api/v1/payments/intents/{payment.PaymentIntentId:D}");
        Assert.NotNull(paymentDetails);

        var eventId = $"evt-pricing-{Guid.NewGuid():N}";
        var payload = new
        {
            eventId,
            eventType = "payment.succeeded",
            providerPaymentIntentId = paymentDetails!.ProviderPaymentIntentId,
            status = "Captured",
            amount = paymentDetails.Amount,
            currency = paymentDetails.Currency,
        };

        var firstWebhook = await client.PostAsJsonAsync("/api/v1/payments/webhooks/Demo", payload);
        var secondWebhook = await client.PostAsJsonAsync("/api/v1/payments/webhooks/Demo", payload);

        Assert.Equal(HttpStatusCode.OK, firstWebhook.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondWebhook.StatusCode);

        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var pricingDbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
                return await pricingDbContext.PromotionRedemptions.CountAsync() == 1;
            },
            "Pricing redemptions were not recorded.");

        using (var scope = factory.Services.CreateScope())
        {
            var pricingDbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
            var coupon = await pricingDbContext.Coupons.SingleAsync(item => item.Code == couponCode);
            var promotion = await pricingDbContext.Promotions.SingleAsync(item => item.Name == "Coupon REDEEM10");

            Assert.Equal(1, await pricingDbContext.PromotionRedemptions.CountAsync());
            Assert.Equal(1, coupon.TimesUsedTotal);
            Assert.Equal(1, promotion.TimesUsedTotal);
        }
    }

    [Fact]
    public async Task CartPriceEndpoint_ShouldApplyFreeShippingPromotion_OnlyToShipping()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Shipping promo product", 40m);
        await SeedActivePromotionAsync(
            factory,
            "Free shipping over 20",
            [new PromotionScopeData(PromotionScopeType.Shipping, null)],
            [new PromotionConditionData(PromotionConditionType.MinSubtotal, PromotionConditionOperator.Gte, "20")],
            [new PromotionBenefitData(PromotionBenefitType.FreeShipping, null, null, null, false)]);

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/cart/price",
            new
            {
                customerId = (string?)null,
                isAuthenticated = false,
                lines = new[]
                {
                    new { productId = product.ProductId, variantId = product.VariantId, quantity = 1 },
                },
                couponCode = (string?)null,
                shipping = new { shippingMethodCode = "standard", currency = "EUR", priceAmount = 5.99m },
                bypassCache = true,
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CartPayload>();
        Assert.NotNull(payload);
        Assert.Equal(40m, payload!.SubtotalAmount);
        Assert.Equal(5.99m, payload.ShippingDiscountTotalAmount);
        Assert.Equal(40m, payload.GrandTotalAmount);
    }

    [Fact]
    public async Task CouponValidationEndpoint_ShouldReturnValidCoupon()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var product = await CreateProductAsync(factory, client, "Coupon validation product", 55m);
        var couponCode = await SeedCouponPromotionAsync(factory, "CHECK15", 15m);

        var response = await client.PostAsJsonAsync(
            "/api/v1/pricing/coupons/validate",
            new
            {
                code = couponCode,
                customerId = (string?)null,
                isAuthenticated = false,
                lines = new[]
                {
                    new { productId = product.ProductId, variantId = product.VariantId, quantity = 1 },
                },
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CouponValidationPayload>();
        Assert.NotNull(payload);
        Assert.True(payload!.IsValid);
        Assert.Equal(couponCode, payload.Code);
        Assert.Equal("Coupon CHECK15", payload.PromotionName);
    }

    [Fact]
    public async Task PricingAdminEndpoints_ShouldRequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/pricing/price-lists");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PromotionEndpoints_ShouldCreateListAndActivate_WhenAuthenticated()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync(
            "/api/v1/pricing/promotions",
            new
            {
                name = "Admin created promo",
                code = "ADMINPROMO",
                type = 0,
                description = "Created from integration test",
                priority = 50,
                isExclusive = false,
                allowWithCoupons = true,
                startAtUtc = (DateTime?)null,
                endAtUtc = (DateTime?)null,
                usageLimitTotal = (int?)null,
                usageLimitPerCustomer = (int?)null,
                scopes = new[]
                {
                    new { scopeType = 0, targetId = (Guid?)null },
                },
                conditions = Array.Empty<object>(),
                benefits = new[]
                {
                    new { benefitType = 0, valueAmount = (decimal?)null, valuePercent = 10m, maxDiscountAmount = (decimal?)null, applyPerUnit = false },
                },
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createPayload = await createResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(createPayload);

        var listResponse = await client.GetFromJsonAsync<IReadOnlyCollection<PromotionListPayload>>("/api/v1/pricing/promotions");
        Assert.NotNull(listResponse);
        Assert.Contains(listResponse!, promotion => promotion.Id == createPayload!.Id && promotion.Status == 0);

        var activateResponse = await client.PostAsync($"/api/v1/pricing/promotions/{createPayload!.Id:D}/activate", content: null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);

        var promotionResponse = await client.GetFromJsonAsync<PromotionListPayload>($"/api/v1/pricing/promotions/{createPayload.Id:D}");
        Assert.NotNull(promotionResponse);
        Assert.Equal(1, promotionResponse!.Status);
    }

    private static AppHostWebApplicationFactory CreateManualCaptureFactory()
    {
        return new AppHostWebApplicationFactory(new Dictionary<string, string?>
        {
            ["Payments:Demo:AutoCaptureOnCreate"] = "false",
            ["Payments:Demo:SimulateRequiresAction"] = "false",
            ["Payments:Demo:SimulateFailureRate"] = "0",
        });
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"pricing-admin-{Guid.NewGuid():N}@example.com";
        const string password = "Pricing!Pass123";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CustomersModuleExtensions.RegisterRequest(
                Email: email,
                Password: password,
                FirstName: "Pricing",
                LastName: "Admin",
                PhoneNumber: "+359888000000"));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest(email, password, RememberMe: false));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    private static async Task<ProductInfo> CreateProductAsync(
        AppHostWebApplicationFactory factory,
        HttpClient client,
        string name,
        decimal amount)
    {
        var categorySlug = $"pricing-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: amount,
                IsActive: true,
                Brand: "Contoso",
                Sku: $"SKU-{Guid.NewGuid():N}",
                ImageUrl: "/images/test.png",
                IsInStock: true,
                CategorySlug: categorySlug,
                CategoryName: "Pricing Tests"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);

        await WaitForStockItemAsync(client, payload!.Id);

        await using var scope = factory.Services.CreateAsyncScope();
        var catalogDbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var product = await catalogDbContext.Products.SingleAsync(item => item.Id == payload.Id);
        return new ProductInfo(product.Id, product.DefaultVariantId, product.DefaultCategoryId);
    }

    private static async Task SeedVariantPriceAsync(
        AppHostWebApplicationFactory factory,
        Guid variantId,
        decimal basePriceAmount,
        decimal? compareAtPriceAmount)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var pricingDbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var priceListId = await EnsureDefaultPriceListAsync(pricingDbContext);

        var variantPriceResult = VariantPrice.Create(
            priceListId,
            variantId,
            basePriceAmount,
            compareAtPriceAmount,
            "EUR",
            true,
            null,
            null,
            DateTime.UtcNow);
        Assert.True(variantPriceResult.IsSuccess);

        await pricingDbContext.VariantPrices.AddAsync(variantPriceResult.Value);
        await pricingDbContext.SaveChangesAsync();
    }

    private static async Task SeedActivePromotionAsync(
        AppHostWebApplicationFactory factory,
        string name,
        IReadOnlyCollection<PromotionScopeData> scopes,
        IReadOnlyCollection<PromotionConditionData> conditions,
        IReadOnlyCollection<PromotionBenefitData> benefits)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var pricingDbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();

        var promotionResult = Promotion.Create(
            name,
            null,
            benefits.Any(benefit => benefit.BenefitType == PromotionBenefitType.FreeShipping)
                ? PromotionType.FreeShipping
                : PromotionType.PercentageOff,
            null,
            50,
            false,
            true,
            null,
            null,
            null,
            null,
            scopes,
            conditions,
            benefits,
            DateTime.UtcNow);
        Assert.True(promotionResult.IsSuccess);

        var activateResult = promotionResult.Value.Activate(DateTime.UtcNow);
        Assert.True(activateResult.IsSuccess);

        await pricingDbContext.Promotions.AddAsync(promotionResult.Value);
        await pricingDbContext.SaveChangesAsync();
    }

    private static async Task<string> SeedCouponPromotionAsync(
        AppHostWebApplicationFactory factory,
        string couponCode,
        decimal discountAmount)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var pricingDbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();

        var promotionResult = Promotion.Create(
            $"Coupon {couponCode}",
            couponCode,
            PromotionType.FixedAmountOff,
            "Cart coupon",
            25,
            false,
            true,
            null,
            null,
            null,
            null,
            [new PromotionScopeData(PromotionScopeType.Cart, null)],
            [],
            [new PromotionBenefitData(PromotionBenefitType.FixedAmountOff, discountAmount, null, null, false)],
            DateTime.UtcNow);
        Assert.True(promotionResult.IsSuccess);
        Assert.True(promotionResult.Value.Activate(DateTime.UtcNow).IsSuccess);

        var couponResult = Coupon.Create(
            couponCode,
            $"Coupon {couponCode}",
            promotionResult.Value.Id,
            null,
            null,
            null,
            null,
            DateTime.UtcNow);
        Assert.True(couponResult.IsSuccess);

        await pricingDbContext.Promotions.AddAsync(promotionResult.Value);
        await pricingDbContext.Coupons.AddAsync(couponResult.Value);
        await pricingDbContext.SaveChangesAsync();

        return couponResult.Value.Code;
    }

    private static async Task<Guid> EnsureDefaultPriceListAsync(PricingDbContext pricingDbContext)
    {
        var existing = await pricingDbContext.PriceLists.FirstOrDefaultAsync(item => item.Code == "default");
        if (existing is not null)
        {
            return existing.Id;
        }

        var result = PriceList.Create("Default EUR", "default", "EUR", true, true, 100, DateTime.UtcNow);
        Assert.True(result.IsSuccess);

        await pricingDbContext.PriceLists.AddAsync(result.Value);
        await pricingDbContext.SaveChangesAsync();
        return result.Value.Id;
    }

    private static async Task<CreatePaymentIntentPayload> CreatePaymentIntentAsync(
        HttpClient client,
        Guid orderId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/intents")
        {
            Content = JsonContent.Create(new
            {
                orderId,
                provider = "Demo",
                customerEmail = "buyer@example.com",
            }),
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentIntentPayload>();
        Assert.NotNull(payload);
        return payload!;
    }

    private static Task WaitForStockItemAsync(HttpClient client, Guid productId)
    {
        return WaitForConditionAsync(
            async () =>
            {
                var response = await client.GetAsync($"/api/v1/inventory/products/{productId:D}");
                return response.StatusCode == HttpStatusCode.OK;
            },
            $"Inventory stock item was not provisioned for product {productId:D}.");
    }

    private static async Task WaitForConditionAsync(Func<Task<bool>> predicate, string timeoutMessage)
    {
        for (var retry = 0; retry < 60; retry++)
        {
            if (await predicate())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        throw new TimeoutException(timeoutMessage);
    }

    private sealed record ProductInfo(Guid ProductId, Guid VariantId, Guid? CategoryId);

    private sealed record CreateEntityPayload(Guid Id);

    private sealed record VariantPricingPayload(
        Guid VariantId,
        string Currency,
        decimal BasePriceAmount,
        decimal? CompareAtPriceAmount,
        decimal EffectivePriceAmount,
        bool IsDiscounted,
        IReadOnlyCollection<object> AppliedDiscounts);

    private sealed record CartPayload(
        Guid Id,
        string CustomerId,
        string? AppliedCouponCode,
        string Currency,
        decimal SubtotalBeforeDiscountAmount,
        decimal SubtotalAmount,
        decimal LineDiscountTotalAmount,
        decimal CartDiscountTotalAmount,
        decimal GrandTotalAmount,
        IReadOnlyCollection<CartLinePayload> Lines,
        IReadOnlyCollection<object> AppliedDiscounts,
        IReadOnlyCollection<string> Messages,
        decimal ShippingDiscountTotalAmount = 0m);

    private sealed record CartLinePayload(
        Guid ProductId,
        Guid VariantId,
        string? Sku,
        string ProductName,
        string? VariantName,
        string? SelectedOptionsJson,
        string? ImageUrl,
        string Currency,
        decimal BaseUnitAmount,
        decimal? CompareAtUnitAmount,
        decimal FinalUnitAmount,
        decimal LineTotalAmount,
        decimal DiscountTotalAmount,
        int Quantity);

    private sealed record OrderPayload(
        Guid Id,
        string CustomerId,
        string Currency,
        decimal SubtotalBeforeDiscountAmount,
        decimal SubtotalAmount,
        decimal LineDiscountTotalAmount,
        decimal CartDiscountTotalAmount,
        decimal ShippingPriceAmount,
        decimal ShippingDiscountTotalAmount,
        string ShippingCurrency,
        string ShippingMethodCode,
        string ShippingMethodName,
        decimal TotalAmount,
        string? AppliedCouponsJson,
        string? AppliedPromotionsJson,
        string Status,
        string FulfillmentStatus,
        DateTime PlacedAtUtc,
        object ShippingAddress,
        object BillingAddress,
        IReadOnlyCollection<OrderLinePayload> Lines);

    private sealed record OrderLinePayload(
        Guid ProductId,
        Guid VariantId,
        string? Sku,
        string Name,
        string? VariantName,
        string? SelectedOptionsJson,
        string Currency,
        decimal BaseUnitAmount,
        decimal FinalUnitAmount,
        decimal? CompareAtPriceAmount,
        decimal DiscountTotalAmount,
        string? AppliedDiscountsJson,
        int Quantity);

    private sealed record CreatePaymentIntentPayload(
        Guid PaymentIntentId,
        string Provider,
        string Status,
        string? ClientSecret,
        bool RequiresAction,
        string? RedirectUrl);

    private sealed record PaymentIntentDetailsPayload(
        Guid Id,
        Guid OrderId,
        Guid? CustomerId,
        string Provider,
        string Status,
        decimal Amount,
        string Currency,
        string? ProviderPaymentIntentId,
        string? ClientSecret,
        string? FailureCode,
        string? FailureMessage,
        DateTime CreatedAtUtc,
        DateTime UpdatedAtUtc,
        DateTime? CompletedAtUtc);

    private sealed record CouponValidationPayload(
        string Code,
        bool IsValid,
        string? ErrorCode,
        string? ErrorMessage,
        string? PromotionName);

    private sealed record PromotionListPayload(Guid Id, string Name, int Status);
}
