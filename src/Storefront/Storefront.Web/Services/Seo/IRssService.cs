namespace Storefront.Web.Services.Seo;

public interface IRssService
{
    Task<string> BuildXmlAsync(CancellationToken cancellationToken);
}
