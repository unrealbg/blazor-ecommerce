using BuildingBlocks.Domain.Results;

namespace Redirects.Domain.RedirectRules;

public static class RedirectPathNormalizer
{
    public static Result<string> NormalizeFromPath(string fromPath)
    {
        return NormalizePath(fromPath, allowQueryString: false);
    }

    public static Result<string> NormalizeToPath(string toPath)
    {
        return NormalizePath(toPath, allowQueryString: true);
    }

    public static string NormalizeForComparison(string pathOrPathAndQuery)
    {
        var withoutQuery = RemoveQuery(pathOrPathAndQuery);
        return EnsurePathFormat(withoutQuery);
    }

    private static Result<string> NormalizePath(string value, bool allowQueryString)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result<string>.Failure(new Error("redirects.path.required", "Redirect path is required."));
        }

        var trimmed = value.Trim();
        var query = string.Empty;

        if (allowQueryString)
        {
            var queryIndex = trimmed.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                query = trimmed[queryIndex..];
                trimmed = trimmed[..queryIndex];
            }
        }
        else
        {
            trimmed = RemoveQuery(trimmed);
        }

        var normalizedPath = EnsurePathFormat(trimmed);
        if (normalizedPath.Length > 450)
        {
            return Result<string>.Failure(new Error("redirects.path.too_long", "Redirect path is too long."));
        }

        if (!allowQueryString)
        {
            return Result<string>.Success(normalizedPath);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            return Result<string>.Success(normalizedPath);
        }

        if (query.Length > 500)
        {
            return Result<string>.Failure(new Error("redirects.path.query_too_long", "Redirect query string is too long."));
        }

        return Result<string>.Success($"{normalizedPath}{query}");
    }

    private static string RemoveQuery(string path)
    {
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        return queryIndex >= 0 ? path[..queryIndex] : path;
    }

    private static string EnsurePathFormat(string path)
    {
        var normalized = path.Trim();

        if (!normalized.StartsWith("/", StringComparison.Ordinal))
        {
            normalized = $"/{normalized}";
        }

        normalized = normalized.ToLowerInvariant();

        if (normalized.Length > 1)
        {
            normalized = normalized.TrimEnd('/');
            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = $"/{normalized}";
            }
        }

        return normalized;
    }
}
