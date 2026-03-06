using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Payments.Infrastructure.Persistence;

namespace Payments.Infrastructure;

public sealed class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PaymentsDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                               ?? "Host=localhost;Port=5432;Database=blazor_ecommerce;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);
        return new PaymentsDbContext(optionsBuilder.Options, new SystemTextJsonEventSerializer());
    }
}
