using BuildingBlocks.Infrastructure.Modules;

namespace BuildingBlocks.Infrastructure.Extensions;

public static class ModuleInfrastructureInitializationExtensions
{
    public static async Task InitializeModulesAsync(
        this IEnumerable<IModuleInfrastructureInstaller> installers,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        foreach (var installer in installers)
        {
            await installer.InitializeAsync(serviceProvider, cancellationToken);
        }
    }

    public static async Task SeedModulesAsync(
        this IEnumerable<IModuleInfrastructureInstaller> installers,
        IServiceProvider serviceProvider,
        string seedMode,
        CancellationToken cancellationToken)
    {
        foreach (var installer in installers.OfType<IModuleInfrastructureSeeder>())
        {
            await installer.SeedAsync(serviceProvider, seedMode, cancellationToken);
        }
    }
}
