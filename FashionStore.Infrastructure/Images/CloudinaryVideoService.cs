using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FashionStore.Application.Abstractions.Videos;
using Microsoft.Extensions.Options;

namespace FashionStore.Infrastructure.Images;

public sealed class CloudinaryVideoService : ICloudinaryVideoService
{
    private const string Folder = "masondelola/promotion-videos";
    private readonly Cloudinary cloudinary;

    public CloudinaryVideoService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret)) { Api = { Secure = true } };
    }

    public async Task<CloudinaryVideoUpload> UploadAsync(byte[] data, string fileName, string contentType, CancellationToken cancellationToken)
    {
        if (data.Length == 0) throw new ArgumentException("Promotion video data is required.", nameof(data));
        await using var stream = new MemoryStream(data, writable: false);
        var result = await cloudinary.UploadAsync(new VideoUploadParams
        {
            File = new FileDescription(fileName, stream), Folder = Folder,
            UseFilename = true, UniqueFilename = true, Overwrite = false
        }, cancellationToken);
        if (result.Error is not null || result.SecureUrl is null)
            throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary did not return a video URL.");
        return new CloudinaryVideoUpload(result.SecureUrl.AbsoluteUri, contentType, result.OriginalFilename ?? fileName, result.Bytes);
    }

    public async Task DeleteAsync(string? videoUrl, CancellationToken cancellationToken)
    {
        var publicId = GetPublicId(videoUrl);
        if (publicId is null) return;
        cancellationToken.ThrowIfCancellationRequested();
        await cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = ResourceType.Video });
    }

    private static string? GetPublicId(string? videoUrl)
    {
        if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri)) return null;
        const string marker = "/upload/";
        var index = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;
        var path = Uri.UnescapeDataString(uri.AbsolutePath[(index + marker.Length)..]);
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 0 && segments[0].StartsWith('v') && segments[0][1..].All(char.IsDigit)) path = string.Join('/', segments.Skip(1));
        var extension = path.LastIndexOf('.');
        return extension > path.LastIndexOf('/') ? path[..extension] : path;
    }
}
