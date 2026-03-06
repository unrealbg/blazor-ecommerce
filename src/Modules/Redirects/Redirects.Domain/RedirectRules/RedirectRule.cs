using BuildingBlocks.Domain.Primitives;
using BuildingBlocks.Domain.Results;

namespace Redirects.Domain.RedirectRules;

public sealed class RedirectRule : AggregateRoot<Guid>
{
    private RedirectRule()
    {
    }

    private RedirectRule(
        Guid id,
        string fromPath,
        string toPath,
        int statusCode,
        DateTime createdAtUtc)
    {
        Id = id;
        FromPath = fromPath;
        ToPath = toPath;
        StatusCode = statusCode;
        IsActive = true;
        HitCount = 0;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public string FromPath { get; private set; } = string.Empty;

    public string ToPath { get; private set; } = string.Empty;

    public int StatusCode { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public bool IsActive { get; private set; }

    public long HitCount { get; private set; }

    public DateTime? LastHitAtUtc { get; private set; }

    public static Result<RedirectRule> Create(
        string fromPath,
        string toPath,
        int statusCode,
        DateTime createdAtUtc)
    {
        var normalizedFromResult = RedirectPathNormalizer.NormalizeFromPath(fromPath);
        if (normalizedFromResult.IsFailure)
        {
            return Result<RedirectRule>.Failure(normalizedFromResult.Error);
        }

        var normalizedToResult = RedirectPathNormalizer.NormalizeToPath(toPath);
        if (normalizedToResult.IsFailure)
        {
            return Result<RedirectRule>.Failure(normalizedToResult.Error);
        }

        if (!IsSupportedStatusCode(statusCode))
        {
            return Result<RedirectRule>.Failure(new Error(
                "redirects.status_code.invalid",
                "Only 301, 302, 307 and 308 redirect status codes are supported."));
        }

        return Result<RedirectRule>.Success(new RedirectRule(
            Guid.NewGuid(),
            normalizedFromResult.Value,
            normalizedToResult.Value,
            statusCode,
            createdAtUtc));
    }

    public Result UpdateTarget(string toPath, int statusCode, DateTime updatedAtUtc)
    {
        var normalizedToResult = RedirectPathNormalizer.NormalizeToPath(toPath);
        if (normalizedToResult.IsFailure)
        {
            return Result.Failure(normalizedToResult.Error);
        }

        if (!IsSupportedStatusCode(statusCode))
        {
            return Result.Failure(new Error(
                "redirects.status_code.invalid",
                "Only 301, 302, 307 and 308 redirect status codes are supported."));
        }

        ToPath = normalizedToResult.Value;
        StatusCode = statusCode;
        UpdatedAtUtc = updatedAtUtc;
        IsActive = true;

        return Result.Success();
    }

    public void Deactivate(DateTime updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void RegisterHits(long hitIncrement, DateTime lastHitAtUtc)
    {
        if (hitIncrement <= 0)
        {
            return;
        }

        HitCount += hitIncrement;
        LastHitAtUtc = lastHitAtUtc;
        UpdatedAtUtc = lastHitAtUtc;
    }

    private static bool IsSupportedStatusCode(int statusCode)
    {
        return statusCode is 301 or 302 or 307 or 308;
    }
}
