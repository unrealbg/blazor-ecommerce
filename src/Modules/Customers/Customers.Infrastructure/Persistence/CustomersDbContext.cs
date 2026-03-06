using BuildingBlocks.Infrastructure.Messaging;
using BuildingBlocks.Infrastructure.Persistence;
using Customers.Application.Customers;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace Customers.Infrastructure.Persistence;

public sealed class CustomersDbContext(
    DbContextOptions<CustomersDbContext> options,
    IEventSerializer eventSerializer)
    : ModuleDbContext(options, eventSerializer), ICustomersUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CustomerSession> CustomerSessions => Set<CustomerSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("customers");
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomersDbContext).Assembly);
    }
}
