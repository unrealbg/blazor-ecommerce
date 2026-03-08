using BuildingBlocks.Infrastructure.Modules;

namespace AppHost.Tests;

public sealed class ModuleInfrastructureLoaderTests
{
    [Fact]
    public void LoadInstallers_OrdersCatalogBeforeDependentModules()
    {
        var installers = ModuleInfrastructureLoader.LoadInstallers()
            .Select((installer, index) => new { installer.ModuleName, index })
            .ToDictionary(item => item.ModuleName, item => item.index, StringComparer.OrdinalIgnoreCase);

        Assert.Contains("Catalog", installers.Keys);
        Assert.Contains("Cart", installers.Keys);
        Assert.Contains("Inventory", installers.Keys);
        Assert.Contains("Orders", installers.Keys);

        Assert.True(installers["Catalog"] < installers["Cart"]);
        Assert.True(installers["Catalog"] < installers["Inventory"]);
        Assert.True(installers["Catalog"] < installers["Orders"]);
    }
}
