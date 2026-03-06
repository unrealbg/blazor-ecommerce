namespace Storefront.Web.Services.Redirects;

public static class StorefrontRedirectExtensions
{
    public static IServiceCollection AddStorefrontRedirects(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddScoped<IStorefrontRedirectLookup, StorefrontRedirectLookup>();
        return services;
    }

    public static IApplicationBuilder UseStorefrontRedirects(this IApplicationBuilder app)
    {
        return app.UseMiddleware<StorefrontRedirectMiddleware>();
    }
}
