using System.Runtime.Loader;

namespace BuildingBlocks.Infrastructure.Modules;

public static class ModuleInfrastructureLoader
{
    public static IReadOnlyCollection<IModuleInfrastructureInstaller> LoadInstallers()
    {
        var installerFiles = Directory
            .GetFiles(AppContext.BaseDirectory, "*.Infrastructure.dll", SearchOption.TopDirectoryOnly)
            .Where(path => !Path.GetFileName(path).StartsWith("BuildingBlocks.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path)
            .ToList();

        var installers = new List<IModuleInfrastructureInstaller>();

        foreach (var file in installerFiles)
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);

            var installerTypes = assembly.GetTypes()
                .Where(type =>
                    type is { IsClass: true, IsAbstract: false } &&
                    typeof(IModuleInfrastructureInstaller).IsAssignableFrom(type));

            foreach (var installerType in installerTypes)
            {
                if (Activator.CreateInstance(installerType) is IModuleInfrastructureInstaller installer)
                {
                    installers.Add(installer);
                }
            }
        }

        return installers;
    }
}
