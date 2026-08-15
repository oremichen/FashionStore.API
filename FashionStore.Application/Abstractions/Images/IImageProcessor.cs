namespace FashionStore.Application.Abstractions.Images;

public sealed class ProcessedImage
{
    public required byte[] Data { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

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
