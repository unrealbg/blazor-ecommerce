using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Search.Domain.Documents;

namespace Search.Infrastructure.Persistence;

public sealed class SearchDbContext(
    DbContextOptions<SearchDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer)
{
    public DbSet<ProductSearchDocument> ProductSearchDocuments => Set<ProductSearchDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("search");
        modelBuilder.HasPostgresExtension("pg_trgm");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SearchDbContext).Assembly);
    }
}
