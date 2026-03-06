using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using Cart.Api;
using Catalog.Api;
using Customers.Api;
using Inventory.Domain.Stock;
using Inventory.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AppHost.Tests;

public sealed class InventoryIntegrationTests
{
    [Fact]
    public async Task AddToCart_Should_CreateActiveReservation()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var productId = await CreateProductAsync(client, "Inventory Add Cart Product", isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        var customerId = $"cart-{Guid.NewGuid():N}";
        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new CartModuleExtensions.AddItemRequest(productId, 2));

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var reservation = inventoryDbContext.StockReservations.Single(item =>
            item.CartId == customerId &&
            item.ProductId == productId &&
            item.Status == StockReservationStatus.Active);

        var stockItem = inventoryDbContext.StockItems.Single(item => item.ProductId == productId);

        Assert.Equal(2, reservation.Quantity);
        Assert.Equal(2, stockItem.ReservedQuantity);
    }

    [Fact]
    public async Task AddToCart_Should_ReturnConflict_WhenStockIsInsufficient()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var productId = await CreateProductAsync(client, "Inventory OOS Product", isInStock: false);
        await WaitForStockItemAsync(factory, productId);

        var customerId = $"cart-{Guid.NewGuid():N}";
        var addResponse = await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new CartModuleExtensions.AddItemRequest(productId, 1));

        Assert.Equal(HttpStatusCode.Conflict, addResponse.StatusCode);

        var payload = await addResponse.Content.ReadAsStringAsync();
        Assert.Contains("Insufficient stock", payload, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InventoryAdjustEndpoint_Should_RequireAuthorization()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var productId = await CreateProductAsync(client, "Inventory Secure Product", isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{productId}/adjust",
            new { quantityDelta = 5, reason = "Unauthorized test" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InventoryAdjustEndpoint_Should_AdjustStockAndWriteMovement_WhenAuthorized()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var productId = await CreateProductAsync(client, "Inventory Adjust Product", isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        var response = await client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{productId}/adjust",
            new { quantityDelta = 5, reason = "Restock for test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stockItem = inventoryDbContext.StockItems.Single(item => item.ProductId == productId);
        Assert.Equal(105, stockItem.OnHandQuantity);

        Assert.Contains(inventoryDbContext.StockMovements, movement =>
            movement.ProductId == productId &&
            movement.Type == StockMovementType.Restock &&
            movement.QuantityDelta == 5);
    }

    [Fact]
    public async Task ReservationExpirationWorker_Should_ExpirePastDueReservations()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        var productId = await CreateProductAsync(client, "Inventory Expiration Product", isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        var customerId = $"cart-{Guid.NewGuid():N}";
        await client.PostAsJsonAsync(
            $"/api/v1/cart/{customerId}/items",
            new CartModuleExtensions.AddItemRequest(productId, 2));

        using (var scope = factory.Services.CreateScope())
        {
            var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var reservation = inventoryDbContext.StockReservations.Single(item =>
                item.CartId == customerId &&
                item.ProductId == productId &&
                item.Status == StockReservationStatus.Active);

            var property = typeof(StockReservation).GetProperty(nameof(StockReservation.ExpiresAtUtc));
            Assert.NotNull(property);
            property!.SetValue(reservation, DateTime.UtcNow.AddMinutes(-5));

            await inventoryDbContext.SaveChangesAsync();
        }

        await RunReservationExpirationSweepAsync(factory.Services);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<InventoryDbContext>();

        Assert.Contains(verifyDbContext.StockReservations, reservation =>
            reservation.CartId == customerId &&
            reservation.ProductId == productId &&
            reservation.Status == StockReservationStatus.Expired);

        var stockItem = verifyDbContext.StockItems.Single(item => item.ProductId == productId);
        Assert.Equal(0, stockItem.ReservedQuantity);
    }

    [Fact]
    public async Task StockChange_Should_UpdateSearchInStockFlag()
    {
        await using var factory = new AppHostWebApplicationFactory();
        using var client = factory.CreateClient();

        await AuthenticateAsync(client);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var productName = $"Inventory Search Product {suffix}";
        var productId = await CreateProductAsync(client, productName, isInStock: true);
        await WaitForStockItemAsync(factory, productId);

        await client.PostAsync("/api/v1/search/rebuild", content: null);

        await client.PostAsJsonAsync(
            $"/api/v1/inventory/products/{productId}/adjust",
            new { quantityDelta = -100, reason = "Sell out" });

        SearchProductsResponse? searchResponse = null;
        for (var retry = 0; retry < 40; retry++)
        {
            searchResponse = await client.GetFromJsonAsync<SearchProductsResponse>($"/api/v1/search/products?q={suffix}");
            var current = searchResponse?.Items.FirstOrDefault(item => item.ProductId == productId);
            if (current is not null && !current.IsInStock)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        Assert.NotNull(searchResponse);
        var item = searchResponse!.Items.FirstOrDefault(resultItem => resultItem.ProductId == productId);
        Assert.NotNull(item);
        Assert.False(item!.IsInStock);
    }

    private static async Task<Guid> CreateProductAsync(HttpClient client, string name, bool isInStock)
    {
        var sku = $"SKU-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync(
            "/api/v1/catalog/products",
            new CatalogModuleExtensions.CreateProductRequest(
                Name: name,
                Description: $"{name} description",
                Currency: "EUR",
                Amount: 10m,
                IsActive: true,
                Brand: "Contoso",
                Sku: sku,
                ImageUrl: "/images/test.png",
                IsInStock: isInStock,
                CategorySlug: "inventory-tests",
                CategoryName: "Inventory Tests"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        Assert.NotNull(payload);
        return payload!.Id;
    }

    private static async Task WaitForStockItemAsync(AppHostWebApplicationFactory factory, Guid productId)
    {
        for (var retry = 0; retry < 40; retry++)
        {
            using var scope = factory.Services.CreateScope();
            var inventoryDbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            var exists = inventoryDbContext.StockItems.Any(item => item.ProductId == productId);
            if (exists)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        throw new TimeoutException($"Stock item provisioning timed out for product '{productId}'.");
    }

    private static async Task RunReservationExpirationSweepAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();
        var expirationWorker = hostedServices.Single(service =>
            string.Equals(service.GetType().Name, "ReservationExpirationWorker", StringComparison.Ordinal));

        var sweepMethod = expirationWorker.GetType().GetMethod(
            "SweepExpiredReservationsAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(sweepMethod);

        var invocation = sweepMethod!.Invoke(expirationWorker, [CancellationToken.None]);
        var task = Assert.IsAssignableFrom<Task>(invocation);
        await task;
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var email = $"inventory-admin-{Guid.NewGuid():N}@example.com";
        const string password = "Inventory!Pass123";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new CustomersModuleExtensions.RegisterRequest(
                Email: email,
                Password: password,
                FirstName: "Inventory",
                LastName: "Admin",
                PhoneNumber: "+359888000000"));

        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new CustomersModuleExtensions.LoginRequest(email, password, RememberMe: false));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Contains(loginResponse.Headers.GetValues("Set-Cookie"), value =>
            value.Contains("blazor-ecommerce-auth", StringComparison.Ordinal));
    }

    private sealed record CreateProductResponse(Guid Id);

    private sealed record SearchProductsResponse(IReadOnlyCollection<SearchProductItem> Items);

    private sealed record SearchProductItem(Guid ProductId, bool IsInStock);
}
