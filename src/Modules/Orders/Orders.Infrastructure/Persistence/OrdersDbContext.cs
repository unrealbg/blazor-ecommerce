using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Orders.Application.Orders;
using Orders.Domain.Orders;

namespace Orders.Infrastructure.Persistence;

public sealed class OrdersDbContext(
    DbContextOptions<OrdersDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), IOrdersUnitOfWork
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrdersDbContext).Assembly);
    }
}
