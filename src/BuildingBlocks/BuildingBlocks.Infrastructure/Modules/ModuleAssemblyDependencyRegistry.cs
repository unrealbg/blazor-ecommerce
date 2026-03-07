using System.Reflection;
using System.Runtime.Loader;

namespace BuildingBlocks.Infrastructure.Modules;

internal static class ModuleAssemblyDependencyRegistry
{
    private static readonly object SyncRoot = new();
    private static readonly Dictionary<string, AssemblyDependencyResolver> Resolvers =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool isRegistered;

    public static void Register(string moduleAssemblyPath)
    {
        lock (SyncRoot)
        {
            if (!Resolvers.ContainsKey(moduleAssemblyPath))
            {
                Resolvers[moduleAssemblyPath] = new AssemblyDependencyResolver(moduleAssemblyPath);
            }

            if (!isRegistered)
            {
                AssemblyLoadContext.Default.Resolving += ResolveAssembly;
                isRegistered = true;
            }
        }
    }

    private static Assembly? ResolveAssembly(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        lock (SyncRoot)
        {
            foreach (var resolver in Resolvers.Values)
            {
                var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                if (!string.IsNullOrWhiteSpace(assemblyPath) && File.Exists(assemblyPath))
                {
                    return context.LoadFromAssemblyPath(assemblyPath);
                }
            }
        }

        return null;
    }
}
