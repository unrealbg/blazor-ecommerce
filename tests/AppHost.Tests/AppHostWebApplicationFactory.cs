using BuildingBlocks.Infrastructure.Persistence;
using Cart.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence;
using Customers.Infrastructure.Identity;
using Customers.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Infrastructure.Persistence;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(
            [
                new KeyValuePair<string, string?>("Infrastructure:SkipInitialization", "true"),
                new KeyValuePair<string, string?>("ConnectionStrings:Postgres", "Host=localhost;Port=5432;Database=test;Username=test;Password=test"),
                new KeyValuePair<string, string?>("ConnectionStrings:Redis", string.Empty),
                new KeyValuePair<string, string?>("Authentication:Jwt:Authority", "https://auth.test/"),
                new KeyValuePair<string, string?>("Authentication:Jwt:Audience", "apphost-tests"),
                new KeyValuePair<string, string?>("Outbox:BatchSize", "20"),
                new KeyValuePair<string, string?>("Outbox:PollingInterval", "00:00:00.2500000"),
            ]);
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
