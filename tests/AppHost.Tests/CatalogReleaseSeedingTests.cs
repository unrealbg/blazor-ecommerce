using BuildingBlocks.Infrastructure.Modules;
using Catalog.Domain.Products;
using Catalog.Infrastructure.Persistence;
using Catalog.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AppHost.Tests;

public sealed class CatalogReleaseSeedingTests
{
    [Fact]
    public async Task NoneMode_Should_NotSeedCatalogProducts()
    {
        await using var factory = new AppHostWebApplicationFactory();

        await SeedAsync(factory, ReleaseSeedModes.None);

        await using var scope = factory.Services.CreateAsyncScope();
        var catalogDbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        Assert.Equal(0, await catalogDbContext.Products.CountAsync());
    }

    [Fact]
    public async Task MinimalMode_Should_SeedCuratedSubset()
    {
        await using var factory = new AppHostWebApplicationFactory();

        await SeedAsync(factory, ReleaseSeedModes.Minimal);

        await using var scope = factory.Services.CreateAsyncScope();
        var catalogDbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var slugs = await catalogDbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Slug)
            .Select(product => product.Slug)
            .ToArrayAsync();

        var adaptiveHeadphones = await catalogDbContext.Products
            .AsNoTracking()
            .Include(product => product.Variants)
            .SingleAsync(product => product.Slug == "adaptive-headphones");

        Assert.Equal(
            ["adaptive-headphones", "mechanical-keyboard", "workspace-dock"],
            slugs);
        Assert.Equal(2, adaptiveHeadphones.Variants.Count);
    }

    [Fact]
    public async Task DemoMode_Should_SeedFullCatalog_AndRemainIdempotent()
    {
        await using var factory = new AppHostWebApplicationFactory();

        await SeedAsync(factory, ReleaseSeedModes.Demo);
        await SeedAsync(factory, ReleaseSeedModes.Demo);

        await using var scope = factory.Services.CreateAsyncScope();
        var catalogDbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var slugs = await catalogDbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Slug)
            .Select(product => product.Slug)
            .ToArrayAsync();

        Assert.Equal(8, slugs.Length);
        Assert.Contains("mechanical-keyboard", slugs, StringComparer.Ordinal);
        Assert.Contains("workspace-monitor-light", slugs, StringComparer.Ordinal);
        Assert.Contains("travel-power-module", slugs, StringComparer.Ordinal);
    }

    private static async Task SeedAsync(AppHostWebApplicationFactory factory, string seedMode)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<CatalogReleaseSeeder>();
        await seeder.SeedAsync(seedMode, CancellationToken.None);
    }
}
