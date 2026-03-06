using BuildingBlocks.Infrastructure.Persistence;
using Cart.Infrastructure.Persistence;
using Catalog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orders.Infrastructure.Persistence;
using Redirects.Infrastructure.Persistence;

namespace AppHost.Tests;

public sealed class AppHostWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string SharedDatabaseName = "apphost-tests-shared";
    private static readonly InMemoryDatabaseRoot DatabaseRoot = new();
    private static readonly IServiceProvider InMemoryServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

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

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<OutboxDbContext>(services);
            ReplaceDbContext<CatalogDbContext>(services);
            ReplaceDbContext<CartDbContext>(services);
            ReplaceDbContext<OrdersDbContext>(services);
            ReplaceDbContext<RedirectsDbContext>(services);
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services)
        where TContext : DbContext
    {
        services.RemoveAll<TContext>();
        services.RemoveAll<DbContextOptions<TContext>>();

        services.AddDbContext<TContext>(options =>
            options
                .UseInMemoryDatabase(SharedDatabaseName, DatabaseRoot)
                .UseInternalServiceProvider(InMemoryServiceProvider));
    }
}
