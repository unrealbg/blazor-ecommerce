using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Cart.Application.Carts;
using Cart.Domain.Carts;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public sealed class CartDbContext(
    DbContextOptions<CartDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), ICartUnitOfWork
{
    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("cart");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CartDbContext).Assembly);
    }
}
