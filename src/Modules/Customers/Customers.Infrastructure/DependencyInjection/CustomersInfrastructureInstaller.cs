using BuildingBlocks.Application.Authorization;
using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Infrastructure.Modules;
using Customers.Application.Auth;
using Customers.Application.Compliance;
using Customers.Application.Customers;
using Customers.Infrastructure.Compliance;
using Customers.Infrastructure.Customers;
using Customers.Infrastructure.Identity;
using Customers.Infrastructure.Persistence;
using Customers.Infrastructure.Sessions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Customers.Infrastructure.DependencyInjection;

public sealed class CustomersInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Customers";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<CustomersDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "customers")));

        services.AddDbContext<IdentityAppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "identity")));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddSignInManager()
            .AddEntityFrameworkStores<IdentityAppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<IdentityOptions>(options =>
        {
            options.SignIn.RequireConfirmedEmail = false;
        });

        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ICustomersUnitOfWork>(provider => provider.GetRequiredService<CustomersDbContext>());
        services.AddScoped<IIdentityAuthService, IdentityAuthService>();
        services.AddScoped<ICustomerDataExportService, CustomerDataExportService>();
        services.AddScoped<ICustomerDataErasureService, CustomerDataErasureService>();
        services.AddScoped<ICustomerCheckoutAccessor, CustomerCheckoutAccessor>();
        services.AddScoped<ICustomerSessionCache, CustomerSessionCache>();
        services.Configure<BackofficeSeedOptions>(configuration.GetSection(BackofficeSeedOptions.SectionName));
        services.AddScoped<IBackofficePermissionService, BackofficePermissionService>();
        services.AddScoped<BackofficeIdentitySeeder>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IdentityAppDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<BackofficeIdentitySeeder>();

        await customersDb.Database.MigrateAsync(cancellationToken);
        await identityDb.Database.MigrateAsync(cancellationToken);
        await seeder.SeedAsync(cancellationToken);
    }
}
