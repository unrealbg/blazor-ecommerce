using BuildingBlocks.Infrastructure.Persistence;
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
using Redirects.Infrastructure.Persistence;
using Search.Infrastructure.Persistence;

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
            this.ReplaceDbContext<CartDbContext>(services);
            this.ReplaceDbContext<OrdersDbContext>(services);
            this.ReplaceDbContext<RedirectsDbContext>(services);
            this.ReplaceDbContext<SearchDbContext>(services);
            this.ReplaceDbContext<CustomersDbContext>(services);
            this.ReplaceDbContext<IdentityAppDbContext>(services);
            this.ReplaceDbContext<InventoryDbContext>(services);
            this.ReplaceDbContext<PaymentsDbContext>(services);
        });
    }

    private void ReplaceDbContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();

        services.AddDbContext<TContext>(options =>
            options
                .UseInMemoryDatabase(this.sharedDatabaseName, this.databaseRoot)
                .UseInternalServiceProvider(InMemoryServiceProvider));
    }
}
