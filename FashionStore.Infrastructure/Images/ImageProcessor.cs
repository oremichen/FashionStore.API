using FashionStore.Domain.Abstractions.Images;
using ImageMagick;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace FashionStore.Infrastructure.Images;

public sealed class ImageProcessor : IImageProcessor
{
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private static readonly string[] HeifContentTypes =
        ["image/heic", "image/heif", "image/heic-sequence", "image/heif-sequence"];

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
        var isHeif = IsHeif(data, contentType, fileName);
        if (!isHeif && !AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPEG, PNG, WebP, HEIC, and HEIF images are supported.", nameof(contentType));
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        try
        {
            var decodableData = isHeif ? ConvertHeifToJpeg(data) : data;
            await using var inputStream = new MemoryStream(decodableData, writable: false);
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
        catch (MagickException exception)
        {
            throw new ArgumentException("The uploaded HEIC/HEIF image is corrupt or invalid.", nameof(data), exception);
        }
    }

    private static byte[] ConvertHeifToJpeg(byte[] data)
    {
        using var image = new MagickImage(data);
        image.AutoOrient();
        image.Strip();
        image.Format = MagickFormat.Jpeg;
        image.Quality = 92;
        return image.ToByteArray();
    }

    private static bool IsHeif(byte[] data, string contentType, string fileName)
    {
        if (HeifContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase)) return true;

        var extension = Path.GetExtension(fileName);
        if (extension.Equals(".heic", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".heif", StringComparison.OrdinalIgnoreCase)) return true;

        // ISO base media files store their major brand after the four-byte box size and "ftyp".
        if (data.Length < 12 || data[4] != (byte)'f' || data[5] != (byte)'t'
            || data[6] != (byte)'y' || data[7] != (byte)'p') return false;

        var brand = System.Text.Encoding.ASCII.GetString(data, 8, 4);
        return brand is "heic" or "heix" or "hevc" or "hevx" or "heim" or "heis" or "mif1" or "msf1";
    }
}
