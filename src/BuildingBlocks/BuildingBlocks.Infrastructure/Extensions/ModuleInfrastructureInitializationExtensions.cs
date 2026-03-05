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
}
