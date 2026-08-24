using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FashionStore.Domain.Abstractions.Images;
using Microsoft.Extensions.Options;

namespace FashionStore.Infrastructure.Images;

public sealed class CloudinaryImageService : ICloudinaryImageService
{
    private const string Folder = "masondelola";
    private readonly Cloudinary cloudinary;

    public CloudinaryImageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.CloudName) || string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.ApiSecret))
            throw new InvalidOperationException("Cloudinary configuration is incomplete.");

        cloudinary = new Cloudinary(new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret))
        {
            Api = { Secure = true }
        };
    }

    public async Task<string> UploadAsync(byte[] data, string fileName, CancellationToken cancellationToken)
    {
        return (await UploadWithMetadataAsync(data, fileName, cancellationToken)).Url;
    }

    public async Task<CloudinaryImageUpload> UploadWithMetadataAsync(byte[] data, string fileName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0) throw new ArgumentException("Image data cannot be empty.", nameof(data));

        await using var stream = new MemoryStream(data, writable: false);
        var result = await cloudinary.UploadAsync(new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = Folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false
        }, cancellationToken);

        if (result.Error is not null || result.SecureUrl is null)
            throw new InvalidOperationException(result.Error?.Message ?? "Cloudinary did not return an image URL.");
        var format = result.Format?.Trim().ToLowerInvariant() ?? string.Empty;
        var contentType = format switch
        {
            "jpg" or "jpeg" => "image/jpeg",
            "png" => "image/png",
            "webp" => "image/webp",
            "gif" => "image/gif",
            "avif" => "image/avif",
            _ when format.Length > 0 => $"image/{format}",
            _ => string.Empty
        };

        return new CloudinaryImageUpload(
            result.SecureUrl.AbsoluteUri,
            contentType,
            result.OriginalFilename ?? fileName ?? string.Empty,
            result.Bytes,
            result.Width,
            result.Height);
    }

    public async Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken)
    {
        var publicId = GetPublicId(imageUrl);
        if (publicId is null) return;
        cancellationToken.ThrowIfCancellationRequested();
        await cloudinary.DestroyAsync(new DeletionParams(publicId) { ResourceType = ResourceType.Image });
    }

    private static string? GetPublicId(string? imageUrl)
    {
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri)) return null;
        var marker = $"/{Folder}/";
        var index = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return null;
        var value = Uri.UnescapeDataString(uri.AbsolutePath[(index + 1)..]);
        var extension = value.LastIndexOf('.');
        return extension > value.LastIndexOf('/') ? value[..extension] : value;
    }
}
