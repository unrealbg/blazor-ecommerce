using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Storefront.Web.Configuration;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;
using Storefront.Web.Services.Seo;

namespace Storefront.Web.Services.Runtime;

public sealed class StorefrontWarmupHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<StorefrontWarmupOptions> options,
    IOptions<StorefrontFeatureFlagsOptions> featureFlags,
    StorefrontWarmupState warmupState,
    ILogger<StorefrontWarmupHostedService> logger)
    : IHostedService
{
    private readonly StorefrontWarmupOptions options = options.Value;
    private readonly StorefrontFeatureFlagsOptions featureFlags = featureFlags.Value;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Enabled || !featureFlags.EnableWarmup)
        {
            logger.LogInformation("Storefront warmup disabled by configuration.");
            return;
        }

        warmupState.MarkStarted();

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)));

        try
        {
            using var scope = scopeFactory.CreateScope();
            var storeApiClient = scope.ServiceProvider.GetRequiredService<IStoreApiClient>();
            var contentClient = scope.ServiceProvider.GetRequiredService<IContentClient>();
            var sitemapService = scope.ServiceProvider.GetRequiredService<ISitemapService>();
            var rssService = scope.ServiceProvider.GetRequiredService<IRssService>();

            var products = await storeApiClient.GetProductsAsync(timeoutCts.Token);
            var activeProducts = products.Where(product => product.IsActive).Take(Math.Max(1, options.MaxFeaturedProducts)).ToArray();

            foreach (var product in activeProducts)
            {
                await storeApiClient.GetProductBySlugAsync(product.Slug, timeoutCts.Token);
                if (featureFlags.EnableReviews)
                {
                    await storeApiClient.GetProductReviewSummaryAsync(product.Id, timeoutCts.Token);
                }
            }

            var warmedCategories = 0;
            if (options.WarmSearch)
            {
                var categorySlugs = products
                    .Where(product => !string.IsNullOrWhiteSpace(product.CategorySlug))
                    .Select(product => product.CategorySlug!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToArray();

                foreach (var categorySlug in categorySlugs)
                {
                    await storeApiClient.SearchProductsAsync(
                        new StoreSearchProductsRequest(null, categorySlug, [], null, null, null, "popular", 1, 24),
                        timeoutCts.Token);
                    warmedCategories++;
                }
            }

            if (options.WarmContent && featureFlags.EnableCmsContent)
            {
                await contentClient.GetAllPublishedBlogSlugs(timeoutCts.Token);
                await contentClient.GetAllPublishedPageSlugs(timeoutCts.Token);
            }

            if (options.WarmSitemap)
            {
                await sitemapService.BuildXmlAsync(timeoutCts.Token);
                await rssService.BuildXmlAsync(timeoutCts.Token);
            }

            warmupState.MarkCompleted(activeProducts.Length, warmedCategories);
            logger.LogInformation(
                "Storefront warmup completed. WarmedProducts={WarmedProducts} WarmedCategories={WarmedCategories}",
                activeProducts.Length,
                warmedCategories);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            warmupState.MarkFailed(exception);
            logger.LogWarning(exception, "Storefront warmup failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}