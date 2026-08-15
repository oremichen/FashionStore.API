namespace FashionStore.Application.Abstractions.Images;

public sealed record ProcessedImage(byte[] Data, string ContentType, string FileName, int Width, int Height);

public interface IImageProcessor
{
    Task<ProcessedImage> CropAndResizeAsync(
        byte[] data,
        string contentType,
        string fileName,
        int width,
        int height,
        bool allowUpscale,
        CancellationToken cancellationToken);
}
