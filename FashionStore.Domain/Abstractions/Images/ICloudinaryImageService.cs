namespace FashionStore.Domain.Abstractions.Images;

public sealed record CloudinaryImageUpload(
    string Url,
    string ContentType,
    string FileName,
    long FileSize,
    int Width,
    int Height);

public interface ICloudinaryImageService
{
    Task<string> UploadAsync(byte[] data, string fileName, CancellationToken cancellationToken);
    Task<CloudinaryImageUpload> UploadWithMetadataAsync(byte[] data, string fileName, CancellationToken cancellationToken);
    Task DeleteAsync(string? imageUrl, CancellationToken cancellationToken);
}
