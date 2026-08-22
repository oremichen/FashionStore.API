namespace FashionStore.Application.Abstractions.Videos;

public sealed record CloudinaryVideoUpload(string Url, string ContentType, string FileName, long FileSize);

public interface ICloudinaryVideoService
{
    Task<CloudinaryVideoUpload> UploadAsync(byte[] data, string fileName, string contentType, CancellationToken cancellationToken);
    Task DeleteAsync(string? videoUrl, CancellationToken cancellationToken);
}
