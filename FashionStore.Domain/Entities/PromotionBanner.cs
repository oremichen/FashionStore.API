namespace FashionStore.Domain.Entities;

public sealed class PromotionBanner
{
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
    public byte[] ImageData { get; private set; } = [];
    public string ImageContentType { get; private set; } = string.Empty;
    public string ImageFileName { get; private set; } = string.Empty;
    public long ImageFileSize { get; private set; }
    public string? ImageUrl { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PromotionBanner Create(string? title, string? subtitle, string? destinationUrl, string? placement, int slot, bool isActive)
    {
        return new PromotionBanner(title, subtitle, destinationUrl, placement, slot, isActive);
    }

    public void SetDetails(string? title, string? subtitle, string? destinationUrl, string? placement, int slot, bool isActive)
    {
        Title = Rules.Optional(title, 150, nameof(title));
        Subtitle = Rules.Optional(subtitle, 250, nameof(subtitle));
        DestinationUrl = Rules.Optional(destinationUrl, 2048, nameof(destinationUrl));
        Placement = Rules.Required(string.IsNullOrWhiteSpace(placement) ? "homepage-banner-grid" : placement, 100, nameof(placement));
        if (slot < 1) throw new ArgumentOutOfRangeException(nameof(slot), "Slot must be greater than zero.");
        Slot = slot;
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImageUrl(string imageUrl, string? contentType = null, string? fileName = null, long fileSize = 0)
    {
        ImageUrl = Rules.Required(imageUrl, 2048, nameof(imageUrl));
        ImageContentType = contentType ?? string.Empty;
        ImageFileName = fileName ?? string.Empty;
        ImageFileSize = fileSize;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
