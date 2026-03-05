using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Application.Carts;
using Microsoft.EntityFrameworkCore;
using CartAggregate = Cart.Domain.Carts.Cart;

namespace Cart.Infrastructure.Persistence;

public sealed class CartDbContext(
    DbContextOptions<CartDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), ICartUnitOfWork
{
    public DbSet<CartAggregate> Carts => Set<CartAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cart");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);
    }
}
