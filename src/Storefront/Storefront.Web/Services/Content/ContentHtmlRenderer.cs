using AngleSharp.Html.Parser;
using Markdig;
using Storefront.Web.Services.Media;

namespace Storefront.Web.Services.Content;

public sealed class ContentHtmlRenderer(
    IMediaUrlService mediaUrlService,
    ILogger<ContentHtmlRenderer> logger)
    : IContentHtmlRenderer
{
    private static readonly HtmlParser HtmlParser = new();
    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string Render(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var html = LooksLikeHtml(content)
            ? content
            : Markdown.ToHtml(content, MarkdownPipeline);

        var document = HtmlParser.ParseDocument($"<html><body>{html}</body></html>");
        var images = document.Images.ToArray();

        foreach (var image in images)
        {
            var source = image.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(source) ||
                !mediaUrlService.TryContentInline(source, 1200, out var proxiedSource))
            {
                logger.LogWarning("Dropping unsupported content image source: {ImageSource}", source);
                image.Remove();
                continue;
            }

            image.SetAttribute("src", proxiedSource);
            image.SetAttribute("loading", "lazy");
            image.SetAttribute("decoding", "async");

            var srcSet = BuildInlineSrcSet(mediaUrlService, source);
            if (!string.IsNullOrWhiteSpace(srcSet))
            {
                image.SetAttribute("srcset", srcSet);
                image.SetAttribute("sizes", "(max-width: 960px) 100vw, 960px");
            }
        }

        return document.Body?.InnerHtml ?? html;
    }

    private static string BuildInlineSrcSet(IMediaUrlService mediaUrlService, string source)
    {
        var variants = new List<string>(3);

        foreach (var width in new[] { 640, 960, 1200 })
        {
            if (mediaUrlService.TryContentInline(source, width, out var variant))
            {
                variants.Add($"{variant} {width}w");
            }
        }

        return string.Join(", ", variants.Distinct(StringComparer.Ordinal));
    }

    private static bool LooksLikeHtml(string value)
    {
        return value.Contains("</", StringComparison.Ordinal) ||
               value.Contains("<img", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("<p", StringComparison.OrdinalIgnoreCase);
    }
}
