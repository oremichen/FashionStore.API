namespace FashionStore.Domain.Entities;

public sealed class MainCarousel
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private MainCarousel() { }

    private MainCarousel(string title, string? subtitle, string buttonText, string? linkUrl, int sortOrder, bool isActive)
    {
        SetDetails(title, subtitle, buttonText, linkUrl, sortOrder, isActive);
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string? Title { get; private set; } = null!;
    public string? Subtitle { get; private set; }
    public string? ButtonText { get; private set; } = null!;
    public string? LinkUrl { get; private set; }
    public byte[] ImageData { get; private set; } = null!;
    public string ImageContentType { get; private set; } = null!;
    public string? ImageFileName { get; private set; }
    public long ImageFileSize { get; private set; }
    public int ImageWidth { get; private set; }
    public int ImageHeight { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static MainCarousel Create(string title, string? subtitle, string buttonText, string? linkUrl, int sortOrder, bool isActive)
    {
        return new MainCarousel(title, subtitle, buttonText, linkUrl, sortOrder, isActive);
    }

    public void SetDetails(string? title, string? subtitle, string? buttonText, string? linkUrl, int sortOrder, bool isActive)
    {
        Title = CatalogRules.Optional(title, 150, nameof(title));
        Subtitle = CatalogRules.Optional(subtitle, 250, nameof(subtitle));
        ButtonText = CatalogRules.Optional(buttonText, 80, nameof(buttonText));
        LinkUrl = CatalogRules.Optional(linkUrl, 2048, nameof(linkUrl));
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImage(byte[] data, string contentType, string fileName, int width, int height)
    {
        if (width != 1920 || height != 750)
            throw new ArgumentException("Carousel images must be exactly 1920 × 750 pixels.");
        var image = ImageRules.Validate(data, contentType, fileName, AllowedImageTypes);
        ImageData = image.Data;
        ImageContentType = image.ContentType;
        ImageFileName = image.FileName;
        ImageFileSize = image.FileSize;
        ImageWidth = width;
        ImageHeight = height;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
