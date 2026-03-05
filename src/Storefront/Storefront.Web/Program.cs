using Microsoft.Extensions.Options;
using Storefront.Web.Components;
using Storefront.Web.Services;
using Storefront.Web.Services.Api;
using Storefront.Web.Services.Customer;
using Storefront.Web.Services.Seo;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection(ApiOptions.SectionName));
builder.Services.Configure<SeoOptions>(builder.Configuration.GetSection(SeoOptions.SectionName));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPageMetadataService, PageMetadataService>();
builder.Services.AddScoped<ISitemapService, SitemapService>();
builder.Services.AddScoped<ICustomerContext, CookieCustomerContext>();
builder.Services.AddScoped<CartState>();

builder.Services.AddHttpClient<IStoreApiClient, StoreApiClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.MapGet("/robots.txt", (IOptions<SeoOptions> options) =>
{
    var baseUrl = options.Value.SiteBaseUrl.TrimEnd('/');
    var robots = $"User-agent: *{Environment.NewLine}Allow: /{Environment.NewLine}Sitemap: {baseUrl}/sitemap.xml";
    return Results.Text(robots, "text/plain");
});

app.MapGet("/sitemap.xml", async (ISitemapService sitemapService, CancellationToken cancellationToken) =>
{
    var xml = await sitemapService.BuildXmlAsync(cancellationToken);
    return Results.Text(xml, "application/xml");
});

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
