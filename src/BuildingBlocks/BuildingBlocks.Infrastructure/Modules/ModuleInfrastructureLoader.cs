using System.Reflection;
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
            ModuleAssemblyDependencyRegistry.Register(file);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
            PreloadLocalDependencies(assembly);

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

    private static void PreloadLocalDependencies(Assembly assembly)
    {
        var pending = new Queue<Assembly>();
        pending.Enqueue(assembly);

        while (pending.Count > 0)
        {
            var currentAssembly = pending.Dequeue();
            foreach (var reference in currentAssembly.GetReferencedAssemblies())
            {
                if (IsAssemblyLoaded(reference))
                {
                    continue;
                }

                var candidatePath = Path.Combine(AppContext.BaseDirectory, $"{reference.Name}.dll");
                if (!File.Exists(candidatePath))
                {
                    continue;
                }

                try
                {
                    var loadedAssembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(candidatePath);
                    pending.Enqueue(loadedAssembly);
                }
                catch (FileLoadException)
                {
                    // Already loaded by the default context.
                }
            }
        }
    }

    private static bool IsAssemblyLoaded(AssemblyName assemblyName)
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(loadedAssembly => AssemblyName.ReferenceMatchesDefinition(loadedAssembly.GetName(), assemblyName));
    }
}
