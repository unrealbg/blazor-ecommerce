using System.Globalization;
using System.Xml.Linq;
using Storefront.Web.Services.Content;

namespace Storefront.Web.Services.Seo;

public sealed class RssService(
    IContentClient contentClient,
    IPageMetadataService pageMetadataService)
    : IRssService
{
    public async Task<string> BuildXmlAsync(CancellationToken cancellationToken)
    {
        var result = await contentClient.GetBlogPosts(1, 200, cancellationToken);
        var posts = result.IsSuccess && result.Value is not null
            ? result.Value
                .Where(post => post.PublishedAt is not null)
                .OrderByDescending(post => post.PublishedAt)
                .ToList()
            : [];

        var channel = new XElement(
            "channel",
            new XElement("title", "Blazor Commerce Blog"),
            new XElement("link", pageMetadataService.BuildAbsoluteUrl("/blog")),
            new XElement("description", "Latest e-commerce news and updates."),
            new XElement("language", "en-US"),
            new XElement("lastBuildDate", DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture)));

        foreach (var post in posts)
        {
            var postUrl = pageMetadataService.BuildAbsoluteUrl($"/blog/{post.Slug}");
            channel.Add(
                new XElement(
                    "item",
                    new XElement("title", post.Title),
                    new XElement("link", postUrl),
                    new XElement("guid", postUrl),
                    new XElement("description", post.Excerpt),
                    new XElement("pubDate", post.PublishedAt?.ToString("R", CultureInfo.InvariantCulture) ?? string.Empty)));
        }

        var rss = new XElement("rss", new XAttribute("version", "2.0"), channel);
        var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rss);
        return document.ToString(SaveOptions.DisableFormatting);
    }
}
