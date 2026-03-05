using BuildingBlocks.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Infrastructure.Persistence;

public sealed class OutboxDbContext(DbContextOptions<OutboxDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("shared");

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            OutboxModelConfiguration.Configure(builder);
            builder.ToTable("outbox_messages", "shared");
        });

        base.OnModelCreating(modelBuilder);
    }
}
