using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Modules;

public interface IModuleInfrastructureInstaller
{
    string ModuleName { get; }

    IReadOnlyCollection<string> DependsOnModules => Array.Empty<string>();

    void AddInfrastructure(IServiceCollection services, IConfiguration configuration);

    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
