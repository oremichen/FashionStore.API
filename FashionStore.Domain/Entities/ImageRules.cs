namespace FashionStore.Domain.Entities;

public sealed record ValidatedImage(byte[] Data, string ContentType, string FileName, long FileSize);

public static class ImageRules
{
    public const long MaximumFileSize = 5 * 1024 * 1024;

    public static ValidatedImage Validate(
        byte[] data,
        string contentType,
        string fileName,
        IReadOnlyCollection<string> allowedContentTypes,
        long maximumFileSize = MaximumFileSize)
    {
        if (data is null || data.Length == 0)
            throw new ArgumentException("Image cannot be empty.", nameof(data));
        if (data.LongLength > maximumFileSize)
            throw new ArgumentException($"Image cannot exceed {maximumFileSize / (1024 * 1024)} MB.", nameof(data));

        var normalizedContentType = contentType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!allowedContentTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Supported image types are: {string.Join(", ", allowedContentTypes)}.", nameof(contentType));

        var safeFileName = Rules.Required(Path.GetFileName(fileName), 255, nameof(fileName));
        return new ValidatedImage(data.ToArray(), normalizedContentType, safeFileName, data.LongLength);
    }
}
