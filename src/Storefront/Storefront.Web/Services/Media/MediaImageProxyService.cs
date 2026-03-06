using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Storefront.Web.Services.Media;

[SuppressMessage("Style", "SA1204:Static members should appear before instance members", Justification = "Keeps helper methods close to usage.")]
public sealed class MediaImageProxyService(
    IMediaSourceResolver sourceResolver,
    IMediaSourceFetcher sourceFetcher,
    IWebHostEnvironment hostEnvironment,
    IOptions<MediaOptions> mediaOptions,
    ILogger<MediaImageProxyService> logger)
    : IMediaImageProxyService
{
    private const string CacheVersion = "v1";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly MediaOptions options = mediaOptions.Value;
    private readonly string cacheDirectory = ResolveCacheDirectory(hostEnvironment.ContentRootPath, mediaOptions.Value.CachePath);

    public async Task<MediaProxyPayload> GetImageAsync(
        MediaImageRequest request,
        string? acceptHeader,
        CancellationToken cancellationToken)
    {
        var normalizedWidth = NormalizeDimension(request.Width, nameof(request.Width));
        var normalizedHeight = NormalizeDimension(request.Height, nameof(request.Height));
        if (normalizedWidth is null && normalizedHeight is null)
        {
            throw new MediaRequestException(StatusCodes.Status400BadRequest, "Either width or height must be provided.");
        }

        var sourceResolution = sourceResolver.Resolve(request.Source, request.Origin);
        if (!sourceResolution.IsSuccess || sourceResolution.SourceUri is null)
        {
            throw new MediaRequestException(
                sourceResolution.ErrorStatusCode ?? StatusCodes.Status400BadRequest,
                sourceResolution.ErrorMessage ?? "Invalid image source.");
        }

        var outputFormat = ResolveOutputFormat(request.Format, acceptHeader, sourceResolution.SourceUri, this.options);
        var cacheSeed = BuildCacheSeed(
            sourceResolution.SourceUri,
            normalizedWidth,
            normalizedHeight,
            request.Fit,
            outputFormat,
            this.options);

        var fileExtension = GetFileExtension(outputFormat);
        var fileHash = ComputeHash(cacheSeed);
        var imagePath = Path.Combine(this.cacheDirectory, $"{fileHash}.{fileExtension}");
        var metadataPath = imagePath + ".meta.json";

        var cachedPayload = await this.TryGetCachedPayloadAsync(imagePath, metadataPath, cancellationToken);
        if (cachedPayload is not null)
        {
            return cachedPayload;
        }

        var keyLock = KeyLocks.GetOrAdd(imagePath, static _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            cachedPayload = await this.TryGetCachedPayloadAsync(imagePath, metadataPath, cancellationToken);
            if (cachedPayload is not null)
            {
                return cachedPayload;
            }

            var fetchResult = await sourceFetcher.FetchAsync(
                sourceResolution.SourceUri,
                Math.Max(1, this.options.MaxSourceBytes),
                TimeSpan.FromSeconds(Math.Clamp(this.options.FetchTimeoutSeconds, 1, 120)),
                cancellationToken);

            if (fetchResult.Status == MediaSourceFetchStatus.NotFound)
            {
                throw new MediaRequestException(StatusCodes.Status404NotFound, "Source image was not found.");
            }

            if (fetchResult.Status == MediaSourceFetchStatus.TooLarge)
            {
                throw new MediaRequestException(StatusCodes.Status400BadRequest, "Source image exceeds allowed size.");
            }

            if (fetchResult.Status != MediaSourceFetchStatus.Success || fetchResult.Content is null)
            {
                throw new MediaRequestException(StatusCodes.Status500InternalServerError, "Failed to fetch source image.");
            }

            if (!string.IsNullOrWhiteSpace(fetchResult.ContentType) &&
                !fetchResult.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                throw new MediaRequestException(StatusCodes.Status400BadRequest, "Source content is not an image.");
            }

            MediaProxyPayload payload;
            try
            {
                payload = await this.TransformAndStoreAsync(
                    fetchResult,
                    outputFormat,
                    normalizedWidth,
                    normalizedHeight,
                    request.Fit,
                    cacheSeed,
                    imagePath,
                    metadataPath,
                    cancellationToken);
            }
            catch (UnknownImageFormatException exception)
            {
                logger.LogWarning(exception, "Source payload is not a supported image for {Source}", sourceResolution.SourceUri);
                throw new MediaRequestException(StatusCodes.Status400BadRequest, "Unsupported image format.");
            }
            catch (MediaRequestException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Image processing failed for {Source}", sourceResolution.SourceUri);
                throw new MediaRequestException(StatusCodes.Status500InternalServerError, "Image processing failed.");
            }

            return payload;
        }
        finally
        {
            keyLock.Release();
        }
    }

    private async Task<MediaProxyPayload> TransformAndStoreAsync(
        MediaSourceFetchResult fetchResult,
        MediaOutputFormat outputFormat,
        int? requestedWidth,
        int? requestedHeight,
        MediaFitMode fitMode,
        string cacheSeed,
        string imagePath,
        string metadataPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(this.cacheDirectory);

        await using var sourceStream = new MemoryStream(fetchResult.Content!);
        using var image = await Image.LoadAsync(sourceStream, cancellationToken);

        image.Mutate(context => context.AutoOrient());

        if (requestedWidth is not null || requestedHeight is not null)
        {
            var targetSize = BuildTargetSize(
                image.Width,
                image.Height,
                requestedWidth,
                requestedHeight,
                this.options.AllowUpscale);

            var resizeMode = ResolveResizeMode(fitMode, requestedWidth, requestedHeight);
            var resizeOptions = new ResizeOptions
            {
                Mode = resizeMode,
                Size = targetSize,
                Sampler = KnownResamplers.Lanczos3,
                Compand = true,
                PadColor = Color.Transparent,
            };

            image.Mutate(context => context.Resize(resizeOptions));
        }

        StripMetadata(image);

        var contentType = GetContentType(outputFormat);
        var quality = GetQuality(outputFormat, this.options);
        var etagSeed = $"{cacheSeed}|lm:{fetchResult.LastModified?.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture) ?? "na"}|q:{quality}|{CacheVersion}";
        var etag = $"\"{ComputeHash(etagSeed)}\"";

        var temporaryPath = $"{imagePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var output = File.Create(temporaryPath))
            {
                var encoder = BuildEncoder(outputFormat, this.options);
                await image.SaveAsync(output, encoder, cancellationToken);
            }

            File.Move(temporaryPath, imagePath, overwrite: true);

            var metadata = new CachedMediaMetadata(contentType, etag);
            var metadataJson = JsonSerializer.Serialize(metadata, SerializerOptions);
            await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }

        return new MediaProxyPayload(imagePath, contentType, etag, false);
    }

    private async Task<MediaProxyPayload?> TryGetCachedPayloadAsync(
        string imagePath,
        string metadataPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(imagePath) || !File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var metadataJson = await File.ReadAllTextAsync(metadataPath, cancellationToken);
            var metadata = JsonSerializer.Deserialize<CachedMediaMetadata>(metadataJson, SerializerOptions);
            if (metadata is null ||
                string.IsNullOrWhiteSpace(metadata.ContentType) ||
                string.IsNullOrWhiteSpace(metadata.ETag))
            {
                return null;
            }

            return new MediaProxyPayload(imagePath, metadata.ContentType, metadata.ETag, true);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read media cache metadata from {MetadataPath}", metadataPath);
            return null;
        }
    }

    private static string ResolveCacheDirectory(string contentRootPath, string configuredCachePath)
    {
        if (string.IsNullOrWhiteSpace(configuredCachePath))
        {
            return Path.Combine(contentRootPath, "cache", "media");
        }

        return Path.IsPathRooted(configuredCachePath)
            ? configuredCachePath
            : Path.Combine(contentRootPath, configuredCachePath);
    }

    private static int? NormalizeDimension(int? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (value <= 0)
        {
            throw new MediaRequestException(StatusCodes.Status400BadRequest, $"{parameterName} must be greater than 0.");
        }

        if (value > 5000)
        {
            throw new MediaRequestException(StatusCodes.Status400BadRequest, $"{parameterName} is too large.");
        }

        return value;
    }

    private static string BuildCacheSeed(
        Uri sourceUri,
        int? width,
        int? height,
        MediaFitMode fitMode,
        MediaOutputFormat outputFormat,
        MediaOptions options)
    {
        var quality = GetQuality(outputFormat, options);
        return $"{CacheVersion}|src:{sourceUri.AbsoluteUri}|w:{width?.ToString(CultureInfo.InvariantCulture) ?? "na"}|h:{height?.ToString(CultureInfo.InvariantCulture) ?? "na"}|fit:{fitMode}|fmt:{outputFormat}|q:{quality}|up:{options.AllowUpscale}";
    }

    private static string GetFileExtension(MediaOutputFormat outputFormat)
    {
        return outputFormat switch
        {
            MediaOutputFormat.Webp => "webp",
            MediaOutputFormat.Png => "png",
            _ => "jpg",
        };
    }

    private static string GetContentType(MediaOutputFormat outputFormat)
    {
        return outputFormat switch
        {
            MediaOutputFormat.Webp => "image/webp",
            MediaOutputFormat.Png => "image/png",
            _ => "image/jpeg",
        };
    }

    private static IImageEncoder BuildEncoder(MediaOutputFormat outputFormat, MediaOptions options)
    {
        return outputFormat switch
        {
            MediaOutputFormat.Webp => new WebpEncoder
            {
                Quality = Math.Clamp(options.DefaultQualityWebp, 1, 100),
            },
            MediaOutputFormat.Png => new PngEncoder(),
            _ => new JpegEncoder
            {
                Quality = Math.Clamp(options.DefaultQualityJpeg, 1, 100),
            },
        };
    }

    private static int GetQuality(MediaOutputFormat outputFormat, MediaOptions options)
    {
        return outputFormat switch
        {
            MediaOutputFormat.Webp => Math.Clamp(options.DefaultQualityWebp, 1, 100),
            MediaOutputFormat.Png => 100,
            _ => Math.Clamp(options.DefaultQualityJpeg, 1, 100),
        };
    }

    private static MediaOutputFormat ResolveOutputFormat(
        MediaOutputFormat requestedFormat,
        string? acceptHeader,
        Uri sourceUri,
        MediaOptions options)
    {
        if (requestedFormat == MediaOutputFormat.Avif && !options.EnableAvif)
        {
            throw new MediaRequestException(StatusCodes.Status400BadRequest, "AVIF output is disabled.");
        }

        if (requestedFormat == MediaOutputFormat.Avif)
        {
            return MediaOutputFormat.Webp;
        }

        if (requestedFormat != MediaOutputFormat.Auto)
        {
            return requestedFormat;
        }

        var normalizedAccept = acceptHeader ?? string.Empty;
        if (options.EnableAvif &&
            normalizedAccept.Contains("image/avif", StringComparison.OrdinalIgnoreCase))
        {
            return MediaOutputFormat.Webp;
        }

        if (normalizedAccept.Contains("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            return MediaOutputFormat.Webp;
        }

        return sourceUri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            ? MediaOutputFormat.Png
            : MediaOutputFormat.Jpeg;
    }

    private static ResizeMode ResolveResizeMode(MediaFitMode fitMode, int? requestedWidth, int? requestedHeight)
    {
        if (requestedWidth is null || requestedHeight is null)
        {
            return ResizeMode.Max;
        }

        return fitMode switch
        {
            MediaFitMode.Cover => ResizeMode.Crop,
            MediaFitMode.Contain => ResizeMode.Pad,
            _ => ResizeMode.Max,
        };
    }

    private static Size BuildTargetSize(
        int sourceWidth,
        int sourceHeight,
        int? requestedWidth,
        int? requestedHeight,
        bool allowUpscale)
    {
        var targetWidth = requestedWidth ?? (int)Math.Round(sourceWidth * (requestedHeight!.Value / (double)sourceHeight));
        var targetHeight = requestedHeight ?? (int)Math.Round(sourceHeight * (requestedWidth!.Value / (double)sourceWidth));

        if (!allowUpscale)
        {
            targetWidth = Math.Min(targetWidth, sourceWidth);
            targetHeight = Math.Min(targetHeight, sourceHeight);
        }

        targetWidth = Math.Max(1, targetWidth);
        targetHeight = Math.Max(1, targetHeight);

        return new Size(targetWidth, targetHeight);
    }

    private static void StripMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.XmpProfile = null;
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record CachedMediaMetadata(string ContentType, string ETag);
}
