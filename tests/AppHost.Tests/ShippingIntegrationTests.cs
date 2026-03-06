using System.Net;
using System.Net.Http.Json;
using Catalog.Api;
using Customers.Api;
using Microsoft.Extensions.DependencyInjection;
using Orders.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class ShippingIntegrationTests
{
    [Fact]
    public async Task QuoteEndpoint_Should_ReturnMethods_ForConfiguredDestination()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);
        await EnsureShippingConfigurationAsync(client);

        var quoteResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/quotes",
            new
            {
                countryCode = "BG",
                subtotalAmount = 80m,
                currency = "EUR",
            });

        Assert.Equal(HttpStatusCode.OK, quoteResponse.StatusCode);
        var payload = await quoteResponse.Content.ReadFromJsonAsync<ShippingQuotePayload>();
        Assert.NotNull(payload);
        Assert.NotEmpty(payload!.Methods);
        Assert.Contains(payload.Methods, method => method.Code == "standard");
    }

    [Fact]
    public async Task QuoteEndpoint_Should_ReturnNotFound_WhenZoneMissing()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/shipping/quotes",
            new
            {
                countryCode = "US",
                subtotalAmount = 50m,
                currency = "EUR",
            });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_Should_IncludeShippingInOrderTotal_AndPaymentAmount()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);
        await EnsureShippingConfigurationAsync(client);

        var productId = await CreateProductAsync(client, "Shipping checkout product");
        var customerId = $"shipping-{Guid.NewGuid():N}";

        var addToCartResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(productId, 1));
        await AssertStatusCodeAsync(addToCartResponse, HttpStatusCode.Created);

        var checkoutIdempotencyKey = Guid.NewGuid().ToString("N");
        using var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new
            {
                cartSessionId = customerId,
                email = $"guest-{Guid.NewGuid():N}@example.com",
                shippingMethodCode = "standard",
                shippingAddress = new
                {
                    firstName = "Alex",
                    lastName = "Mercer",
                    street = "Ship Street 1",
                    city = "Sofia",
                    postalCode = "1000",
                    country = "BG",
                    phone = "+359888000000",
                },
                billingAddress = new
                {
                    firstName = "Alex",
                    lastName = "Mercer",
                    street = "Bill Street 1",
                    city = "Sofia",
                    postalCode = "1000",
                    country = "BG",
                    phone = "+359888000000",
                },
            }),
        };
        checkoutRequest.Headers.Add("Idempotency-Key", checkoutIdempotencyKey);

        var checkoutResponse = await client.SendAsync(checkoutRequest);
        await AssertStatusCodeAsync(checkoutResponse, HttpStatusCode.Created);
        var checkoutPayload = await checkoutResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(checkoutPayload);

        var orderResponse = await client.GetFromJsonAsync<OrderPayload>($"/api/v1/orders/{checkoutPayload!.Id:D}");
        Assert.NotNull(orderResponse);
        Assert.Equal("standard", orderResponse!.ShippingMethodCode);
        Assert.Equal(5.99m, orderResponse.ShippingPriceAmount);
        Assert.Equal(45.99m, orderResponse.TotalAmount);

        using var paymentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/intents")
        {
            Content = JsonContent.Create(new { orderId = orderResponse.Id, provider = "Demo", customerEmail = "buyer@example.com" }),
        };
        paymentRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var paymentResponse = await client.SendAsync(paymentRequest);
        await AssertStatusCodeAsync(paymentResponse, HttpStatusCode.OK);
        var paymentPayload = await paymentResponse.Content.ReadFromJsonAsync<PaymentIntentPayload>();
        Assert.NotNull(paymentPayload);
        Assert.Equal("Captured", paymentPayload!.Status);

        var paymentDetails = await client.GetFromJsonAsync<PaymentIntentDetailsPayload>(
            $"/api/v1/payments/intents/{paymentPayload.PaymentIntentId:D}");
        Assert.NotNull(paymentDetails);
        Assert.Equal(45.99m, paymentDetails!.Amount);
    }

    [Fact]
    public async Task CreateShipment_Should_Succeed_ForPaidOrder()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);
        await EnsureShippingConfigurationAsync(client);

        var orderId = await CreatePaidOrderAsync(client);
        var createShipmentResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/shipments",
            new { orderId, shippingMethodCode = "standard" });

        Assert.Equal(HttpStatusCode.Created, createShipmentResponse.StatusCode);
        var payload = await createShipmentResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);

        var shipment = await client.GetFromJsonAsync<ShipmentPayload>($"/api/v1/shipping/shipments/{payload!.Id:D}");
        Assert.NotNull(shipment);
        Assert.Equal(orderId, shipment!.OrderId);
        Assert.Contains(shipment.Status, new[] { "Pending", "LabelCreated" });
    }

    [Fact]
    public async Task ShippingWebhookReplay_Should_BeIgnored()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);
        await EnsureShippingConfigurationAsync(client);

        var orderId = await CreatePaidOrderAsync(client);
        var shipmentId = await CreateShipmentAsync(client, orderId);
        var eventId = $"carrier-{Guid.NewGuid():N}";
        var payload = new
        {
            eventId,
            eventType = "shipment.status.updated",
            shipmentId,
            status = "Shipped",
            message = "Shipped",
        };

        var first = await client.PostAsJsonAsync("/api/v1/shipping/webhooks/DemoCarrier", payload);
        var second = await client.PostAsJsonAsync("/api/v1/shipping/webhooks/DemoCarrier", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var firstResult = await first.Content.ReadFromJsonAsync<WebhookResponse>();
        var secondResult = await second.Content.ReadFromJsonAsync<WebhookResponse>();
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.True(firstResult!.Processed);
        Assert.False(secondResult!.Processed);

        using var scope = factory.Services.CreateScope();
        var shippingDbContext = scope.ServiceProvider.GetRequiredService<ShippingDbContext>();
        Assert.Equal(
            1,
            shippingDbContext.CarrierWebhookInboxMessages.Count(message =>
                message.Provider == "DemoCarrier" && message.ExternalEventId == eventId));
    }

    [Fact]
    public async Task DeliveredWebhook_Should_UpdateOrderFulfillmentStatus()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);
        await EnsureShippingConfigurationAsync(client);

        var orderId = await CreatePaidOrderAsync(client);
        var shipmentId = await CreateShipmentAsync(client, orderId);

        var shippedResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/webhooks/DemoCarrier",
            new
            {
                eventId = $"carrier-shipped-{Guid.NewGuid():N}",
                eventType = "shipment.status.updated",
                shipmentId,
                status = "Shipped",
                message = "Shipped",
            });

        Assert.Equal(HttpStatusCode.OK, shippedResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            "/api/v1/shipping/webhooks/DemoCarrier",
            new
            {
                eventId = $"carrier-delivered-{Guid.NewGuid():N}",
                eventType = "shipment.delivered",
                shipmentId,
                status = "Delivered",
                message = "Delivered",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await WaitForConditionAsync(
            async () =>
            {
                var order = await client.GetFromJsonAsync<OrderPayload>($"/api/v1/orders/{orderId:D}");
                return order is not null && string.Equals(order.FulfillmentStatus, "Fulfilled", StringComparison.Ordinal);
            },
            "Order fulfillment status was not updated to Fulfilled.");
    }

    [Fact]
    public async Task CreateShipmentEndpoint_Should_RequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/shipping/shipments",
            new { orderId = Guid.NewGuid(), shippingMethodCode = "standard" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<Guid> CreatePaidOrderAsync(HttpClient client)
    {
        var productId = await CreateProductAsync(client, $"Shipping paid product {Guid.NewGuid():N}");
        var customerId = $"shipping-cart-{Guid.NewGuid():N}";

        var addToCartResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new Cart.Api.CartModuleExtensions.AddItemRequest(productId, 1));
        await AssertStatusCodeAsync(addToCartResponse, HttpStatusCode.Created);

        var checkoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/orders/checkout")
        {
            Content = JsonContent.Create(new
            {
                cartSessionId = customerId,
                email = $"guest-{Guid.NewGuid():N}@example.com",
                shippingMethodCode = "standard",
                shippingAddress = new
                {
                    firstName = "Alex",
                    lastName = "Mercer",
                    street = "Ship Street 1",
                    city = "Sofia",
                    postalCode = "1000",
                    country = "BG",
                    phone = "+359888000000",
                },
                billingAddress = new
                {
                    firstName = "Alex",
                    lastName = "Mercer",
                    street = "Bill Street 1",
                    city = "Sofia",
                    postalCode = "1000",
                    country = "BG",
                    phone = "+359888000000",
                },
            }),
        };
        checkoutRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var checkoutResponse = await client.SendAsync(checkoutRequest);
        await AssertStatusCodeAsync(checkoutResponse, HttpStatusCode.Created);
        var checkoutPayload = await checkoutResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(checkoutPayload);

        using var paymentRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/intents")
        {
            Content = JsonContent.Create(new
            {
                orderId = checkoutPayload!.Id,
                provider = "Demo",
                customerEmail = "buyer@example.com",
            }),
        };
        paymentRequest.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

        var paymentResponse = await client.SendAsync(paymentRequest);
        await AssertStatusCodeAsync(paymentResponse, HttpStatusCode.OK);

        await WaitForConditionAsync(
            async () =>
            {
                var order = await client.GetFromJsonAsync<OrderPayload>($"/api/v1/orders/{checkoutPayload.Id:D}");
                return order is not null && string.Equals(order.Status, "Paid", StringComparison.Ordinal);
            },
            "Order was not marked as Paid.");

        return checkoutPayload.Id;
    }

    private static async Task<Guid> CreateShipmentAsync(HttpClient client, Guid orderId)
    {
        var createShipmentResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/shipments",
            new { orderId, shippingMethodCode = "standard" });
        await AssertStatusCodeAsync(createShipmentResponse, HttpStatusCode.Created);

        var payload = await createShipmentResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);
        return payload!.Id;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: 40m,
                IsActive: true,
                Brand: "Contoso",
                Sku: null,
                ImageUrl: "/images/test.png",
                IsInStock: true,
                CategorySlug: "shipping-tests",
                CategoryName: "Shipping Tests"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(payload);
        await WaitForStockItemAsync(client, payload!.Id);
        return payload.Id;
    }

    private static async Task EnsureShippingConfigurationAsync(HttpClient client)
    {
        var methodResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/methods",
            new
            {
                code = "standard",
                name = "Standard Delivery",
                description = "Standard shipping",
                provider = "DemoCarrier",
                type = "Delivery",
                basePriceAmount = 5.99m,
                currency = "EUR",
                supportsTracking = true,
                supportsPickupPoint = false,
                estimatedMinDays = 2,
                estimatedMaxDays = 4,
                priority = 10,
            });
        await AssertStatusCodeAsync(methodResponse, HttpStatusCode.Created);
        var methodPayload = await methodResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(methodPayload);

        var zoneResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/zones",
            new
            {
                code = "eu",
                name = "Europe",
                countryCodes = new[] { "BG", "DE", "FR" },
            });
        await AssertStatusCodeAsync(zoneResponse, HttpStatusCode.Created);
        var zonePayload = await zoneResponse.Content.ReadFromJsonAsync<CreateEntityPayload>();
        Assert.NotNull(zonePayload);

        var ruleResponse = await client.PostAsJsonAsync(
            "/api/v1/shipping/rules",
            new
            {
                shippingMethodId = methodPayload!.Id,
                shippingZoneId = zonePayload!.Id,
                minOrderAmount = (decimal?)null,
                maxOrderAmount = (decimal?)null,
                minWeightKg = (decimal?)null,
                maxWeightKg = (decimal?)null,
                priceAmount = 5.99m,
                freeShippingThresholdAmount = 100m,
                currency = "EUR",
            });

        await AssertStatusCodeAsync(ruleResponse, HttpStatusCode.Created);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"shipping-admin-{Guid.NewGuid():N}@example.com";
        const string password = "Shipping!Pass123";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CustomersModuleExtensions.RegisterRequest(
                Email: email,
                Password: password,
                FirstName: "Shipping",
                LastName: "Admin",
                PhoneNumber: "+359888000000"));
        await AssertStatusCodeAsync(registerResponse, HttpStatusCode.OK);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest(email, password, RememberMe: false));
        await AssertStatusCodeAsync(loginResponse, HttpStatusCode.OK);
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

    private static async Task AssertStatusCodeAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode == expectedStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new Xunit.Sdk.XunitException(
            $"Expected HTTP {(int)expectedStatusCode} ({expectedStatusCode}) but got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private sealed record CreateEntityPayload(Guid Id);

    private sealed record ShippingQuotePayload(IReadOnlyCollection<ShippingQuoteMethodPayload> Methods);

    private sealed record ShippingQuoteMethodPayload(string Code);

    private sealed record OrderPayload(
        Guid Id,
        decimal ShippingPriceAmount,
        string ShippingMethodCode,
        decimal TotalAmount,
        string Status,
        string FulfillmentStatus);

    private sealed record PaymentIntentPayload(Guid PaymentIntentId, string Status);

    private sealed record PaymentIntentDetailsPayload(decimal Amount);

    private sealed record ShipmentPayload(Guid OrderId, string Status);

    private sealed record WebhookResponse(bool Received, bool Processed);
}
