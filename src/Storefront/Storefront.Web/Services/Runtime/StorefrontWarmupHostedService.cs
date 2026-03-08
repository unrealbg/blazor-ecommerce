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

    private static bool ShouldRetry(Exception exception, CancellationToken hostCancellationToken, CancellationToken timeoutToken)
    {
        if (hostCancellationToken.IsCancellationRequested || timeoutToken.IsCancellationRequested)
        {
            return false;
        }

        return exception is HttpRequestException or TimeoutException;
    }

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
        Exception? lastException = null;
        var attempt = 0;

        while (!timeoutCts.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var result = await WarmAsync(timeoutCts.Token);
                warmupState.MarkCompleted(result.WarmedProducts, result.WarmedCategories);
                logger.LogInformation(
                    "Storefront warmup completed. WarmedProducts={WarmedProducts} WarmedCategories={WarmedCategories}",
                    result.WarmedProducts,
                    result.WarmedCategories);
                return;
            }
            catch (Exception exception) when (ShouldRetry(exception, cancellationToken, timeoutCts.Token))
            {
                lastException = exception;
                var delay = TimeSpan.FromSeconds(Math.Min(5, 1 + attempt));
                logger.LogInformation(
                    "Storefront warmup attempt {Attempt} failed with '{ErrorMessage}'. Retrying in {DelaySeconds}s.",
                    attempt,
                    exception.Message,
                    delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, timeoutCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    break;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                lastException = exception;
                break;
            }
        }

        if (lastException is not null && !cancellationToken.IsCancellationRequested)
        {
            warmupState.MarkFailed(lastException);
            logger.LogWarning(lastException, "Storefront warmup failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task<(int WarmedProducts, int WarmedCategories)> WarmAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var storeApiClient = scope.ServiceProvider.GetRequiredService<IStoreApiClient>();
        var contentClient = scope.ServiceProvider.GetRequiredService<IContentClient>();
        var sitemapService = scope.ServiceProvider.GetRequiredService<ISitemapService>();
        var rssService = scope.ServiceProvider.GetRequiredService<IRssService>();

        var products = await storeApiClient.GetProductsAsync(cancellationToken);
        var activeProducts = products.Where(product => product.IsActive).Take(Math.Max(1, options.MaxFeaturedProducts)).ToArray();

        foreach (var product in activeProducts)
        {
            await storeApiClient.GetProductBySlugAsync(product.Slug, cancellationToken);
            if (featureFlags.EnableReviews)
            {
                await storeApiClient.GetProductReviewSummaryAsync(product.Id, cancellationToken);
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
                    cancellationToken);
                warmedCategories++;
            }
        }

        if (options.WarmContent && featureFlags.EnableCmsContent)
        {
            await contentClient.GetAllPublishedBlogSlugs(cancellationToken);
            await contentClient.GetAllPublishedPageSlugs(cancellationToken);
        }

        if (options.WarmSitemap)
        {
            await sitemapService.BuildXmlAsync(cancellationToken);
            await rssService.BuildXmlAsync(cancellationToken);
        }

        return (activeProducts.Length, warmedCategories);
    }
}
