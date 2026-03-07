using Backoffice.Domain.Audit;
using Backoffice.Domain.Notes;
using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Backoffice.Infrastructure.Persistence;

public sealed class BackofficeDbContext(
    DbContextOptions<BackofficeDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer)
{
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<OrderInternalNote> OrderInternalNotes => Set<OrderInternalNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("backoffice");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackofficeDbContext).Assembly);
    }
}
