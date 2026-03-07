using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Application.Carts;
using Cart.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence;
using Customers.Infrastructure.Identity;
using Customers.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Infrastructure.Persistence;
using Payments.Infrastructure.Persistence;
using Pricing.Infrastructure.Persistence;
using Redirects.Infrastructure.Persistence;
using Reviews.Infrastructure.Persistence;
using Search.Infrastructure.Persistence;
using Shipping.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class AppHostWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly IServiceProvider InMemoryServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

    private readonly string sharedDatabaseName = $"apphost-tests-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot databaseRoot = new();
    private readonly IReadOnlyDictionary<string, string?> configurationOverrides;

    public AppHostWebApplicationFactory()
        : this(new Dictionary<string, string?>())
    {
    }

    public AppHostWebApplicationFactory(IReadOnlyDictionary<string, string?> configurationOverrides)
    {
        this.configurationOverrides = configurationOverrides;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var configuration = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Infrastructure:SkipInitialization"] = "true",
                ["ConnectionStrings:Postgres"] = "Host=localhost;Port=5432;Database=test;Username=test;Password=test",
                ["ConnectionStrings:Redis"] = string.Empty,
                ["Observability:EnableConsoleExporter"] = "false",
                ["Observability:ServiceName"] = "blazor-ecommerce-tests",
                ["Authentication:Jwt:Authority"] = "https://auth.test/",
                ["Authentication:Jwt:Audience"] = "apphost-tests",
                ["Outbox:BatchSize"] = "20",
                ["Outbox:PollingInterval"] = "00:00:00.2500000",
            };

            foreach (var overridePair in this.configurationOverrides)
            {
                configuration[overridePair.Key] = overridePair.Value;
            }

            configurationBuilder.AddInMemoryCollection(configuration);
        });

        builder.ConfigureTestServices(services =>
        {
            this.ReplaceDbContext<OutboxDbContext>(services);
            this.ReplaceDbContext<CatalogDbContext>(services);
            this.ReplaceDbContext<CartDbContext>(services, $"{this.sharedDatabaseName}-cart");
            this.ReplaceDbContext<OrdersDbContext>(services);
            this.ReplaceDbContext<RedirectsDbContext>(services);
            this.ReplaceDbContext<SearchDbContext>(services);
            this.ReplaceDbContext<CustomersDbContext>(services);
            this.ReplaceDbContext<IdentityAppDbContext>(services);
            this.ReplaceDbContext<InventoryDbContext>(services);
            this.ReplaceDbContext<PaymentsDbContext>(services);
            this.ReplaceDbContext<PricingDbContext>(services);
            this.ReplaceDbContext<ReviewsDbContext>(services);
            this.ReplaceDbContext<ShippingDbContext>(services);
            services.RemoveAll<ICartCheckoutAccessor>();
            services.AddScoped<ICartCheckoutAccessor, TestCartCheckoutAccessor>();
        });
    }

    private void ReplaceDbContext<TContext>(IServiceCollection services, string? databaseName = null)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();

        services.AddDbContext<TContext>(options =>
            options
                .UseInMemoryDatabase(databaseName ?? this.sharedDatabaseName, this.databaseRoot)
                .UseInternalServiceProvider(InMemoryServiceProvider));
    }

    private sealed class TestCartCheckoutAccessor(
        ICartRepository cartRepository,
        CartDbContext cartDbContext) : ICartCheckoutAccessor
    {
        public async Task<CartCheckoutSnapshot?> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken)
        {
            var cart = await cartRepository.GetByCustomerIdAsync(customerId, cancellationToken);

            if (cart is null)
            {
                return null;
            }

            var lines = cart.Lines
                .Select(line => new CartCheckoutLineSnapshot(
                    line.ProductId,
                    line.VariantId,
                    line.ProductName,
                    line.VariantName,
                    line.Sku,
                    line.ImageUrl,
                    line.SelectedOptionsJson,
                    line.UnitPrice.Currency,
                    line.UnitPrice.Amount,
                    line.Quantity))
                .ToList();

            return new CartCheckoutSnapshot(cart.Id, cart.CustomerId, cart.AppliedCouponCode, lines);
        }

        public async Task ClearCartAsync(Guid cartId, CancellationToken cancellationToken)
        {
            var cart = await cartDbContext.Carts.FirstOrDefaultAsync(item => item.Id == cartId, cancellationToken);
            if (cart is null)
            {
                return;
            }

            cart.Clear();
            cartDbContext.Carts.Remove(cart);
            await cartDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
