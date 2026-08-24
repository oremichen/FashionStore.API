using FashionStore.Infrastructure.Contracts.Abstractions.Images;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace FashionStore.Infrastructure.Images;

public sealed class ImageProcessor : IImageProcessor
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public async Task<ProcessedImage> CropAndResizeAsync(
        byte[] data,
        string contentType,
        string fileName,
        int width,
        int height,
        bool allowUpscale,
        CancellationToken cancellationToken)
    {
        if (data is null || data.Length == 0)
            throw new ArgumentException("Image cannot be empty.", nameof(data));
        if (data.LongLength > ImageRules.MaximumFileSize)
            throw new ArgumentException("Image cannot exceed 5 MB.", nameof(data));
        if (!AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPEG, PNG, and WebP images are supported.", nameof(contentType));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        try
        {
            await using var inputStream = new MemoryStream(data, writable: false);
            using var image = await Image.LoadAsync(inputStream, cancellationToken);

            var outputWidth = width;
            var outputHeight = height;
            if (!allowUpscale)
            {
                var scale = Math.Min(1d, Math.Min(
                    image.Width / (double)width,
                    image.Height / (double)height));
                outputWidth = Math.Max(1, (int)Math.Floor(width * scale));
                outputHeight = Math.Max(1, (int)Math.Floor(height * scale));
            }

            image.Mutate(context => context.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(outputWidth, outputHeight),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center,
                Sampler = KnownResamplers.Lanczos3
            }));

            await using var outputStream = new MemoryStream();
            var encoder = new WebpEncoder
            {
                Quality = 85,
                FileFormat = WebpFileFormatType.Lossy
            };
            await image.SaveAsWebpAsync(outputStream, encoder, cancellationToken);

            var outputFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.webp";
            return new ProcessedImage
            {
                Data = outputStream.ToArray(), ContentType = "image/webp", FileName = outputFileName,
                Width = outputWidth, Height = outputHeight
            };
        }
        catch (UnknownImageFormatException exception)
        {
            throw new ArgumentException("The uploaded file is not a valid supported image.", nameof(data), exception);
        }
        catch (InvalidImageContentException exception)
        {
            throw new ArgumentException("The uploaded image is corrupt or invalid.", nameof(data), exception);
        }
    }
}
