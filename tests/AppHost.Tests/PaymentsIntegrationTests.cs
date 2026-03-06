using System.Net;
using System.Net.Http.Json;
using BuildingBlocks.Domain.Shared;
using Catalog.Api;
using Inventory.Domain.Stock;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Orders.Domain.Orders;
using Orders.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class PaymentsIntegrationTests
{
    [Fact]
    public async Task CreatePaymentIntent_ShouldRequireIdempotencyKey()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/payments/intents",
            new { orderId = Guid.NewGuid(), provider = "Demo" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPaymentIntent_ShouldRequireIdempotencyKey()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsync($"/api/v1/payments/intents/{Guid.NewGuid():D}/confirm", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentIntent_ShouldReturnSameIntent_WhenIdempotencyKeyIsReused()
    {
        await using var factory = CreateManualCaptureFactory();
        using var client = factory.CreateClient();

        var scenario = await PreparePendingOrderScenarioAsync(factory, client, quantity: 1);
        var idempotencyKey = Guid.NewGuid().ToString("N");

        var first = await CreatePaymentIntentAsync(client, scenario.OrderId, idempotencyKey);
        var second = await CreatePaymentIntentAsync(client, scenario.OrderId, idempotencyKey);

        Assert.Equal(first.PaymentIntentId, second.PaymentIntentId);
        Assert.Equal("Pending", first.Status);
        Assert.Equal("Pending", second.Status);
    }

    [Fact]
    public async Task CreatePaymentIntent_ShouldCaptureOrder_AndConsumeInventory_WhenAutoCaptureEnabled()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var scenario = await PreparePendingOrderScenarioAsync(factory, client, quantity: 2);

        var payment = await CreatePaymentIntentAsync(client, scenario.OrderId, Guid.NewGuid().ToString("N"));

        Assert.Equal("Captured", payment.Status);

        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var ordersDbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var order = await ordersDbContext.Orders.FindAsync(scenario.OrderId);
                return order is not null && order.Status == OrderStatus.Paid;
            },
            "Order did not reach Paid status after payment capture.");

        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                return inventoryDbContext.StockReservations.Any(reservation =>
                    reservation.OrderId == scenario.OrderId &&
                    reservation.ProductId == scenario.ProductId &&
                    reservation.Status == StockReservationStatus.Consumed);
            },
            "Inventory reservation was not consumed for paid order.");

        using (var verificationScope = factory.Services.CreateScope())
        {
            var inventoryDbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var stockItem = inventoryDbContext.StockItems.Single(item => item.ProductId == scenario.ProductId);

            Assert.Equal(98, stockItem.OnHandQuantity);
            Assert.Equal(0, stockItem.ReservedQuantity);
        }
    }

    [Fact]
    public async Task ConfirmPaymentIntent_ShouldMarkOrderPaid_ForPendingIntent()
    {
        await using var factory = CreateManualCaptureFactory();
        using var client = factory.CreateClient();

        var scenario = await PreparePendingOrderScenarioAsync(factory, client, quantity: 1);
        var createResult = await CreatePaymentIntentAsync(client, scenario.OrderId, Guid.NewGuid().ToString("N"));

        Assert.Equal("Pending", createResult.Status);

        var confirmResult = await ConfirmPaymentIntentAsync(
            client,
            createResult.PaymentIntentId,
            Guid.NewGuid().ToString("N"));

        Assert.Equal("Captured", confirmResult.Status);

        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var ordersDbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var order = await ordersDbContext.Orders.FindAsync(scenario.OrderId);
                return order is not null && order.Status == OrderStatus.Paid;
            },
            "Order was not marked as Paid after payment confirmation.");
    }

    [Fact]
    public async Task FailedPaymentWebhook_ShouldMarkOrderPaymentFailed_AndReleaseReservations()
    {
        await using var factory = CreateManualCaptureFactory();
        using var client = factory.CreateClient();

        var scenario = await PreparePendingOrderScenarioAsync(factory, client, quantity: 1);
        var createResult = await CreatePaymentIntentAsync(client, scenario.OrderId, Guid.NewGuid().ToString("N"));
        var details = await client.GetFromJsonAsync<PaymentIntentDetailsResponse>(
            $"/api/v1/payments/intents/{createResult.PaymentIntentId:D}");

        Assert.NotNull(details);
        Assert.False(string.IsNullOrWhiteSpace(details!.ProviderPaymentIntentId));

        var eventId = $"evt-failed-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            "/api/v1/payments/webhooks/Demo",
            new
            {
                eventId,
                eventType = "payment.failed",
                providerPaymentIntentId = details.ProviderPaymentIntentId,
                status = "Failed",
                amount = details.Amount,
                currency = details.Currency,
                failureCode = "declined",
                failureMessage = "Card declined",
            });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var ordersDbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var order = await ordersDbContext.Orders.FindAsync(scenario.OrderId);
                return order is not null && order.Status == OrderStatus.PaymentFailed;
            },
            "Order did not move to PaymentFailed after failed webhook.");

        using (var verificationScope = factory.Services.CreateScope())
        {
            var inventoryDbContext = verificationScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            Assert.Contains(inventoryDbContext.StockReservations, reservation =>
                reservation.OrderId == scenario.OrderId &&
                reservation.ProductId == scenario.ProductId &&
                reservation.Status == StockReservationStatus.Released);

            var stockItem = inventoryDbContext.StockItems.Single(item => item.ProductId == scenario.ProductId);
            Assert.Equal(100, stockItem.OnHandQuantity);
            Assert.Equal(0, stockItem.ReservedQuantity);
        }
    }

    [Fact]
    public async Task WebhookReplay_ShouldBeProcessedOnlyOnce()
    {
        await using var factory = CreateManualCaptureFactory();
        using var client = factory.CreateClient();

        var scenario = await PreparePendingOrderScenarioAsync(factory, client, quantity: 1);
        var createResult = await CreatePaymentIntentAsync(client, scenario.OrderId, Guid.NewGuid().ToString("N"));

        var details = await client.GetFromJsonAsync<PaymentIntentDetailsResponse>(
            $"/api/v1/payments/intents/{createResult.PaymentIntentId:D}");
        Assert.NotNull(details);

        var eventId = $"evt-{Guid.NewGuid():N}";
        var payload = new
        {
            eventId,
            eventType = "payment.succeeded",
            providerPaymentIntentId = details!.ProviderPaymentIntentId,
            status = "Captured",
            amount = details.Amount,
            currency = details.Currency,
        };

        var firstResponse = await client.PostAsJsonAsync("/api/v1/payments/webhooks/Demo", payload);
        var secondResponse = await client.PostAsJsonAsync("/api/v1/payments/webhooks/Demo", payload);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<WebhookResponse>();
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<WebhookResponse>();

        Assert.NotNull(firstPayload);
        Assert.NotNull(secondPayload);
        Assert.True(firstPayload!.Processed);
        Assert.False(secondPayload!.Processed);

        using var scope = factory.Services.CreateScope();
        var paymentsDbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();

        Assert.Equal(
            1,
            paymentsDbContext.WebhookInboxMessages.Count(message =>
                message.Provider == "Demo" && message.ExternalEventId == eventId));
    }

    [Fact]
    public async Task RefundEndpoint_ShouldRequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/v1/payments/intents/{Guid.NewGuid():D}/refund",
            new { amount = 5m, reason = "test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private static async Task<PaymentScenario> PreparePendingOrderScenarioAsync(
        AppHostWebApplicationFactory factory,
        HttpClient client,
        int quantity)
    {
        var productId = await CreateProductAsync(client, $"Payments Product {Guid.NewGuid():N}", isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        using var scope = factory.Services.CreateScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var ordersDbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

        var stockItem = inventoryDbContext.StockItems.Single(item => item.ProductId == productId);

        var unitPriceResult = Money.Create("EUR", 25m);
        Assert.True(unitPriceResult.IsSuccess);

        var orderResult = Order.Create(
            customerId: $"customer-{Guid.NewGuid():N}",
            checkoutSessionId: $"session-{Guid.NewGuid():N}",
            lineData:
            [
                new OrderLineData(
                    productId,
                    "Payments test product",
                    unitPriceResult.Value,
                    quantity),
            ],
            placedAtUtc: DateTime.UtcNow);

        Assert.True(orderResult.IsSuccess);
        var order = orderResult.Value;

        var reserveResult = stockItem.Reserve(quantity, DateTime.UtcNow);
        Assert.True(reserveResult.IsSuccess);

        var reservationResult = StockReservation.Create(
            productId,
            stockItem.Sku,
            cartId: $"seed-cart-{Guid.NewGuid():N}",
            customerId: null,
            quantity,
            reservationToken: Guid.NewGuid().ToString("N"),
            expiresAtUtc: DateTime.UtcNow.AddMinutes(30),
            createdAtUtc: DateTime.UtcNow);

        Assert.True(reservationResult.IsSuccess);
        var reservation = reservationResult.Value;

        var assignResult = reservation.AssignToOrder(order.Id, DateTime.UtcNow);
        Assert.True(assignResult.IsSuccess);

        await ordersDbContext.Orders.AddAsync(order);
        await inventoryDbContext.StockReservations.AddAsync(reservation);
        await ordersDbContext.SaveChangesAsync();
        await inventoryDbContext.SaveChangesAsync();

        return new PaymentScenario(productId, order.Id);
    }

    private static async Task<CreatePaymentIntentResponse> CreatePaymentIntentAsync(
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
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        Assert.NotNull(payload);
        return payload!;
    }

    private static async Task<CreatePaymentIntentResponse> ConfirmPaymentIntentAsync(
        HttpClient client,
        Guid paymentIntentId,
        string idempotencyKey)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/v1/payments/intents/{paymentIntentId:D}/confirm");

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreatePaymentIntentResponse>();
        Assert.NotNull(payload);
        return payload!;
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name, bool isInStock)
    {
        var sku = $"PAY-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: 25m,
                IsActive: true,
                Brand: "Contoso",
                Sku: sku,
                ImageUrl: "/images/test.png",
                IsInStock: isInStock,
                CategorySlug: "payments-tests",
                CategoryName: "Payments Tests"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        Assert.NotNull(payload);
        return payload!.Id;
    }

    private static async Task WaitForStockItemAsync(AppHostWebApplicationFactory factory, Guid productId)
    {
        await WaitForConditionAsync(
            async () =>
            {
                using var scope = factory.Services.CreateScope();
                var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
                return inventoryDbContext.StockItems.Any(item => item.ProductId == productId);
            },
            $"Stock item provisioning timed out for product '{productId}'.");
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

    private sealed record PaymentScenario(Guid ProductId, Guid OrderId);

    private sealed record CreateProductResponse(Guid Id);

    private sealed record CreatePaymentIntentResponse(
        Guid PaymentIntentId,
        string Provider,
        string Status,
        string? ClientSecret,
        bool RequiresAction,
        string? RedirectUrl);

    private sealed record PaymentIntentDetailsResponse(
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

    private sealed record WebhookResponse(bool Received, bool Processed);
}
