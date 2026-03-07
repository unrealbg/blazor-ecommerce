using BuildingBlocks.Application.Contracts;
using BuildingBlocks.Domain.Abstractions;
using BuildingBlocks.Infrastructure.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Reviews;
using Reviews.Domain.Reviews;
using Reviews.Infrastructure.Persistence;
using Reviews.Infrastructure.Services;

namespace Reviews.Infrastructure.DependencyInjection;

public sealed class ReviewsInfrastructureInstaller : IModuleInfrastructureInstaller
{
    public string ModuleName => "Reviews";

    public void AddInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
                               ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.Configure<ReviewsModuleOptions>(configuration.GetSection(ReviewsModuleOptions.SectionName));

        services.AddDbContext<ReviewsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "reviews")));

        services.AddScoped<IReviewsService, ReviewsService>();
        services.AddScoped<IReviewSummaryReader, ReviewSummaryReader>();
        services.AddScoped<ICustomerReviewExportReader, ReviewExportReader>();
    }

    public async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReviewsDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
