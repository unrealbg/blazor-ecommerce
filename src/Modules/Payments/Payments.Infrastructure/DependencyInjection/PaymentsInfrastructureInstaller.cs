using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Payments;
using Payments.Application.Providers;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Providers;
using Payments.Infrastructure.Webhooks;

namespace Payments.Infrastructure.DependencyInjection;

public sealed class PaymentsInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Payments";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<PaymentsModuleOptions>(configuration.GetSection(PaymentsModuleOptions.SectionName));
        services.Configure<DemoPaymentProviderOptions>(configuration.GetSection("Payments:Demo"));
        services.Configure<StripePaymentProviderOptions>(configuration.GetSection("Payments:Stripe"));

        services.AddDbContext<PaymentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "payments")));

        services.AddScoped<IPaymentIntentRepository, PaymentIntentRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IWebhookInboxRepository, WebhookInboxRepository>();
        services.AddScoped<IPaymentIdempotencyRepository, PaymentIdempotencyRepository>();
        services.AddScoped<IPaymentsUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<PaymentsDbContext>());

        services.AddScoped<IPaymentProvider, DemoPaymentProvider>();
        services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentWebhookVerifier, PaymentWebhookVerifier>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
