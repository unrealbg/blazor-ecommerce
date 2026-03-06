using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Redirects.Application.RedirectRules;
using Redirects.Domain.RedirectRules;

namespace Redirects.Infrastructure.Persistence;

public sealed class RedirectsDbContext(
    DbContextOptions<RedirectsDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IRedirectsUnitOfWork
{
    public DbSet<RedirectRule> RedirectRules => Set<RedirectRule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("redirects");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RedirectsDbContext).Assembly);
    }
}
