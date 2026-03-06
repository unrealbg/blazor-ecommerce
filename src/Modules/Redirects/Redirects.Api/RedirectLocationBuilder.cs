using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Redirects.Api;

internal static class RedirectLocationBuilder
{
    public static string BuildLocation(string toPath, string? requestQueryString)
    {
        var incomingQuery = requestQueryString ?? string.Empty;
        if (string.IsNullOrWhiteSpace(incomingQuery) || incomingQuery == "?")
        {
            return toPath;
        }

        if (!toPath.Contains('?', StringComparison.Ordinal))
        {
            return $"{toPath}{incomingQuery}";
        }

        var questionMarkIndex = toPath.IndexOf('?', StringComparison.Ordinal);
        var targetPath = toPath[..questionMarkIndex];
        var targetQueryString = toPath[(questionMarkIndex + 1)..];

        var targetQuery = QueryHelpers.ParseQuery($"?{targetQueryString}");
        var incoming = QueryHelpers.ParseQuery(incomingQuery);

        var merged = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in targetQuery)
        {
            merged[key] = value;
        }

        foreach (var (key, value) in incoming)
        {
            if (!merged.ContainsKey(key))
            {
                merged[key] = value;
            }
        }

        var values = merged
            .SelectMany(pair => pair.Value, (pair, value) => new KeyValuePair<string, string?>(pair.Key, value));

        var mergedQueryString = QueryString.Create(values);
        return $"{targetPath}{mergedQueryString}";
    }
}
