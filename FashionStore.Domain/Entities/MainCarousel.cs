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
    public string Title { get; private set; } = null!;
    public string? Subtitle { get; private set; }
    public string ButtonText { get; private set; } = null!;
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

    public void SetDetails(string title, string? subtitle, string buttonText, string? linkUrl, int sortOrder, bool isActive)
    {
        Title = CatalogRules.Required(title, 150, nameof(title));
        Subtitle = CatalogRules.Optional(subtitle, 250, nameof(subtitle));
        ButtonText = CatalogRules.Required(buttonText, 80, nameof(buttonText));
        LinkUrl = CatalogRules.Optional(linkUrl, 2048, nameof(linkUrl));
        if (sortOrder < 0) throw new ArgumentOutOfRangeException(nameof(sortOrder), "Sort order cannot be negative.");
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImage(byte[] data, string contentType, string fileName, int width, int height)
    {
        if (width is < 1280 or > 3840) throw new ArgumentOutOfRangeException(nameof(width), "Image width must be between 1280 and 3840 pixels.");
        if (height is < 500 or > 2160) throw new ArgumentOutOfRangeException(nameof(height), "Image height must be between 500 and 2160 pixels.");
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
