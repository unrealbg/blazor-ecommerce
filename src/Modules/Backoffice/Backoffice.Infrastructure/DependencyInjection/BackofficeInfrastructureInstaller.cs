using Backoffice.Application.Backoffice;
using Backoffice.Infrastructure.Persistence;
using Backoffice.Infrastructure.Services;
using BuildingBlocks.Application.Auditing;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Backoffice.Infrastructure.DependencyInjection;

public sealed class BackofficeInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Backoffice";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<BackofficeDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "backoffice")));

        services.AddScoped<IBackofficeQueryService, BackofficeQueryService>();
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<IOrderInternalNoteService, OrderInternalNoteService>();
        services.AddScoped<IAuditTrail, BackofficeAuditTrail>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackofficeDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
