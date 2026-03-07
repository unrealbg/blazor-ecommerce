using Microsoft.AspNetCore.ResponseCaching;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Storefront.Web.Configuration;
using Storefront.Web.Components;
using Storefront.Web.Services;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Content;
using Storefront.Web.Services.Customer;
using Storefront.Web.Services.Media;
using Storefront.Web.Services.Redirects;
using Storefront.Web.Services.Runtime;
using Storefront.Web.Services.Seo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.Configure<CmsOptions>(builder.Configuration.GetSection(CmsOptions.SectionName));
builder.Services.Configure<SiteOptions>(builder.Configuration.GetSection(SiteOptions.SectionName));
builder.Services.AddOptions<StorefrontCacheOptions>()
    .BindConfiguration(StorefrontCacheOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontFeatureFlagsOptions>()
    .BindConfiguration(StorefrontFeatureFlagsOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<StorefrontWarmupOptions>()
    .BindConfiguration(StorefrontWarmupOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddOptions<BuildMetadataOptions>()
    .BindConfiguration(BuildMetadataOptions.SectionName)
    .ValidateOnStart();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddResponseCaching();
builder.Services.AddScoped<ICanonicalUrlService, CanonicalUrlService>();
builder.Services.AddScoped<IPageMetadataService, PageMetadataService>();
builder.Services.AddScoped<IStructuredDataService, StructuredDataService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<IRssService, RssService>();
builder.Services.AddScoped<ICustomerContext, CookieCustomerContext>();
builder.Services.AddScoped<CartState>();
builder.Services.AddStorefrontRedirects();
builder.Services.AddMediaPipeline(builder.Configuration);
builder.Services.AddSingleton<StorefrontBuildInfo>();
builder.Services.AddSingleton<StorefrontWarmupState>();
builder.Services.AddHostedService<StorefrontWarmupHostedService>();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddDistributedMemoryCache();
}
else
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

builder.Services.AddHttpClient<StoreApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});
builder.Services.AddScoped<IStoreApiClient>(serviceProvider =>
    ActivatorUtilities.CreateInstance<CachedStoreApiClient>(serviceProvider));

builder.Services.AddHttpClient<DirectusContentClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<CmsOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
});
builder.Services.AddScoped<IContentClient>(serviceProvider =>
    ActivatorUtilities.CreateInstance<FeatureFlaggedContentClient>(serviceProvider));

var app = builder.Build();
var buildInfo = app.Services.GetRequiredService<StorefrontBuildInfo>();

app.Logger.LogInformation(
    "Starting storefront {ApplicationName} version {Version} revision {Revision}",
    buildInfo.ApplicationName,
    buildInfo.Version,
    buildInfo.SourceRevisionId ?? "n/a");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStorefrontRedirects();
app.UseResponseCompression();
app.UseResponseCaching();
app.Use(async (context, next) =>
{
    ScheduleCachingPolicy(
        context,
        app.Configuration.GetSection(StorefrontCacheOptions.SectionName).Get<StorefrontCacheOptions>() ?? new StorefrontCacheOptions());

    await next();
});
app.MapMediaEndpoints();

app.MapGet("/version", (
    IWebHostEnvironment environment,
    IOptions<StorefrontFeatureFlagsOptions> featureFlags,
    StorefrontWarmupState warmupState,
    StorefrontBuildInfo info) =>
{
    return Results.Ok(new
    {
        application = info.ApplicationName,
        version = info.Version,
        revision = info.SourceRevisionId,
        buildTimestampUtc = info.BuildTimestampUtc,
        environment = environment.EnvironmentName,
        warmup = warmupState.GetSnapshot(),
        activeFeatureFlags = GetActiveFeatureFlags(featureFlags.Value),
    });
});

app.MapGet("/robots.txt", (IOptions<SiteOptions> options) =>
{
    var baseUrl = options.Value.BaseUrl.TrimEnd('/');
    var robots = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {baseUrl}/sitemap.xml";
    return Results.Text(robots, "text/plain");
});

app.MapGet("/sitemap.xml", async (ISitemapService sitemapService, CancellationToken cancellationToken) =>
{
    var xml = await sitemapService.BuildXmlAsync(cancellationToken);
    return Results.Text(xml, "application/xml");
});

app.MapGet("/rss.xml", async (IRssService rssService, CancellationToken cancellationToken) =>
{
    var xml = await rssService.BuildXmlAsync(cancellationToken);
    return Results.Text(xml, "application/rss+xml");
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

static IReadOnlyCollection<string> GetActiveFeatureFlags(StorefrontFeatureFlagsOptions options)
{
    var activeFlags = new List<string>();
    if (options.EnableReviews)
    {
        activeFlags.Add(nameof(StorefrontFeatureFlagsOptions.EnableReviews));
    }

    if (options.EnableCmsContent)
    {
        activeFlags.Add(nameof(StorefrontFeatureFlagsOptions.EnableCmsContent));
    }

    if (options.EnableSearchSuggestions)
    {
        activeFlags.Add(nameof(StorefrontFeatureFlagsOptions.EnableSearchSuggestions));
    }

    if (options.EnableWarmup)
    {
        activeFlags.Add(nameof(StorefrontFeatureFlagsOptions.EnableWarmup));
    }

    return activeFlags;
}

static void ScheduleCachingPolicy(HttpContext context, StorefrontCacheOptions options)
{
    if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
    {
        return;
    }

    var path = context.Request.Path;
    var hasAuthCookie = context.Request.Cookies.ContainsKey("blazor-ecommerce-auth");

    if (hasAuthCookie ||
        path.StartsWithSegments("/account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/checkout", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/cart", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.OnStarting(static state =>
        {
            var httpContext = (HttpContext)state;
            httpContext.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                NoStore = true,
                NoCache = true,
            };

            return Task.CompletedTask;
        }, context);
        return;
    }

    var ttl = ResolveTtl(path, options);
    if (ttl is null)
    {
        return;
    }

    context.Response.Headers.Append("Vary", "Accept-Encoding");
    context.Response.OnStarting(static state =>
    {
        var (httpContext, maxAge) = ((HttpContext HttpContext, TimeSpan MaxAge))state;
        httpContext.Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
        {
            Public = true,
            MaxAge = maxAge,
        };

        return Task.CompletedTask;
    }, (context, ttl.Value));

    var responseCachingFeature = context.Features.Get<IResponseCachingFeature>();
    if (responseCachingFeature is null)
    {
        return;
    }

    if (path.StartsWithSegments("/search", StringComparison.OrdinalIgnoreCase))
    {
        responseCachingFeature.VaryByQueryKeys = ["q", "categorySlug", "brand", "minPrice", "maxPrice", "inStock", "sort", "page"];
    }
    else if (path.StartsWithSegments("/category", StringComparison.OrdinalIgnoreCase))
    {
        responseCachingFeature.VaryByQueryKeys = ["brand", "minPrice", "maxPrice", "inStock", "sort", "page"];
    }
    else if (path.StartsWithSegments("/blog", StringComparison.OrdinalIgnoreCase))
    {
        responseCachingFeature.VaryByQueryKeys = ["page"];
    }
}

static TimeSpan? ResolveTtl(PathString path, StorefrontCacheOptions options)
{
    if (path == "/")
    {
        return TimeSpan.FromSeconds(Math.Max(5, options.HomePageSeconds));
    }

    if (path.Equals("/robots.txt", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(30, options.RobotsSeconds));
    }

    if (path.Equals("/sitemap.xml", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(30, options.SitemapSeconds));
    }

    if (path.Equals("/rss.xml", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(30, options.RssSeconds));
    }

    if (path.StartsWithSegments("/category", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(5, options.CategoryPageSeconds));
    }

    if (path.StartsWithSegments("/product", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(5, options.ProductPageSeconds));
    }

    if (path.StartsWithSegments("/search", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(5, options.SearchPageSeconds));
    }

    if (path.StartsWithSegments("/blog", StringComparison.OrdinalIgnoreCase) || path.StartsWithSegments("/p", StringComparison.OrdinalIgnoreCase))
    {
        return TimeSpan.FromSeconds(Math.Max(5, options.ContentPageSeconds));
    }

    return null;
}

public partial class Program;
