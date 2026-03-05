using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public abstract class ModuleDbContext(
    DbContextOptions options,
    IEventSerializer eventSerializer)
    : DbContext(options)
{
    private readonly IEventSerializer _eventSerializer = eventSerializer;

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddDomainEventsToOutbox();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        AddDomainEventsToOutbox();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            OutboxModelConfiguration.Configure(builder);
            builder.ToTable("outbox_messages", "shared", tableBuilder => tableBuilder.ExcludeFromMigrations());
        });

        base.OnModelCreating(modelBuilder);
    }

    private void AddDomainEventsToOutbox()
    {
        var domainEvents = ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count != 0)
            .SelectMany(entity =>
            {
                var pendingEvents = entity.DomainEvents.ToList();
                entity.ClearDomainEvents();
                return pendingEvents;
            })
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(OutboxMessage.Create(domainEvent, _eventSerializer));
        }
    }
}
