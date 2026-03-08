using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Orders;
using Orders.Infrastructure.Orders;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure.DependencyInjection;

public sealed class OrdersInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Orders";

    public IReadOnlyCollection<string> DependsOnModules => ["Catalog"];

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<OrdersDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "orders")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICheckoutIdempotencyRepository, CheckoutIdempotencyRepository>();
        services.AddScoped<IOrderAuditRepository, OrderAuditRepository>();
        services.AddScoped<ICartCheckoutAccessor, CartCheckoutAccessor>();
        services.AddScoped<IOrderPaymentService, OrderPaymentService>();
        services.AddScoped<IOrderPricingReader, OrderPricingReader>();
        services.AddScoped<IOrderReviewVerifier, OrderReviewVerifier>();
        services.AddScoped<ICustomerOrderExportReader, OrderExportReader>();
        services.AddScoped<IOrderFulfillmentService, OrderFulfillmentService>();
        services.AddScoped<IOrdersUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<OrdersDbContext>());
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
