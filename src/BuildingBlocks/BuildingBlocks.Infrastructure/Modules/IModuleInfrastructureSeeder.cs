namespace BuildingBlocks.Infrastructure.Modules;

public interface IModuleInfrastructureSeeder
{
    Task SeedAsync(IServiceProvider serviceProvider, string seedMode, CancellationToken cancellationToken);
}
