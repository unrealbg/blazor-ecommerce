namespace Storefront.Web.Services.Seo;

public interface ISitemapService
{
    Task<string> BuildXmlAsync(CancellationToken cancellationToken);
}
