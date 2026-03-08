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

        return OrderInstallers(installers);
    }

    private static IReadOnlyCollection<IModuleInfrastructureInstaller> OrderInstallers(
        IReadOnlyCollection<IModuleInfrastructureInstaller> installers)
    {
        var installersByModule = new Dictionary<string, IModuleInfrastructureInstaller>(StringComparer.OrdinalIgnoreCase);
        var remainingDependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var installer in installers)
        {
            if (!installersByModule.TryAdd(installer.ModuleName, installer))
            {
                throw new InvalidOperationException(
                    $"Duplicate infrastructure installer detected for module '{installer.ModuleName}'.");
            }
        }

        foreach (var installer in installers)
        {
            var dependencies = installer.DependsOnModules
                .Where(dependency => !string.Equals(dependency, installer.ModuleName, StringComparison.OrdinalIgnoreCase))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingDependencies = dependencies
                .Where(dependency => !installersByModule.ContainsKey(dependency))
                .OrderBy(dependency => dependency, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (missingDependencies.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Module '{installer.ModuleName}' depends on missing installers: {string.Join(", ", missingDependencies)}.");
            }

            remainingDependencies[installer.ModuleName] = dependencies;
        }

        var readyModules = new SortedSet<string>(
            remainingDependencies
                .Where(pair => pair.Value.Count == 0)
                .Select(pair => pair.Key),
            StringComparer.OrdinalIgnoreCase);

        var orderedInstallers = new List<IModuleInfrastructureInstaller>(installers.Count);

        while (readyModules.Count > 0)
        {
            var moduleName = readyModules.Min!;
            readyModules.Remove(moduleName);

            orderedInstallers.Add(installersByModule[moduleName]);
            remainingDependencies.Remove(moduleName);

            var newlyReadyModules = remainingDependencies
                .Where(pair => pair.Value.Remove(moduleName) && pair.Value.Count == 0)
                .Select(pair => pair.Key)
                .ToArray();

            foreach (var readyModule in newlyReadyModules)
            {
                readyModules.Add(readyModule);
            }
        }

        if (remainingDependencies.Count > 0)
        {
            var unresolvedModules = remainingDependencies.Keys
                .OrderBy(moduleName => moduleName, StringComparer.OrdinalIgnoreCase);

            throw new InvalidOperationException(
                $"Cyclic infrastructure installer dependencies detected for modules: {string.Join(", ", unresolvedModules)}.");
        }

        return orderedInstallers;
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
