namespace FashionStore.Domain.Entities;

public sealed class PromotionBanner
{
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/png", "image/webp"];
    private PromotionBanner() { }

    private PromotionBanner(string? title, string? subtitle, string? destinationUrl, string? placement, int slot, bool isActive)
    {
        SetDetails(title, subtitle, destinationUrl, placement, slot, isActive);
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string? Title { get; private set; }
    public string? Subtitle { get; private set; }
    public string? DestinationUrl { get; private set; }
    public string Placement { get; private set; } = "homepage-banner-grid";
    public int Slot { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] ImageData { get; private set; } = null!;
    public string ImageContentType { get; private set; } = null!;
    public string ImageFileName { get; private set; } = null!;
    public long ImageFileSize { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PromotionBanner Create(string? title, string? subtitle, string? destinationUrl, string? placement, int slot, bool isActive)
    {
        return new PromotionBanner(title, subtitle, destinationUrl, placement, slot, isActive);
    }

    public void SetDetails(string? title, string? subtitle, string? destinationUrl, string? placement, int slot, bool isActive)
    {
        Title = CatalogRules.Optional(title, 150, nameof(title));
        Subtitle = CatalogRules.Optional(subtitle, 250, nameof(subtitle));
        DestinationUrl = CatalogRules.Optional(destinationUrl, 2048, nameof(destinationUrl));
        Placement = CatalogRules.Required(string.IsNullOrWhiteSpace(placement) ? "homepage-banner-grid" : placement, 100, nameof(placement));
        if (slot < 1) throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be greater than zero.");
        Slot = slot;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImage(byte[] data, string contentType, string fileName)
    {
        var image = ImageRules.Validate(data, contentType, fileName, AllowedImageTypes);
        ImageData = image.Data;
        ImageContentType = image.ContentType;
        ImageFileName = image.FileName;
        ImageFileSize = image.FileSize;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
