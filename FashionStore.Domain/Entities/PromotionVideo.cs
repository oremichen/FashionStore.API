namespace FashionStore.Domain.Entities;

public sealed class PromotionVideo
{
    private PromotionVideo() { }

    private PromotionVideo(string title, string slug, bool isActive, DateTimeOffset? expiresAt)
    {
        SetDetails(title, slug, isActive, expiresAt);
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Id { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? VideoUrl { get; private set; }
    public string VideoContentType { get; private set; } = string.Empty;
    public string VideoFileName { get; private set; } = string.Empty;
    public long VideoFileSize { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public bool HasExpired
    {
        get
        {
            return ExpiresAt.HasValue && ExpiresAt.Value <= DateTimeOffset.UtcNow;
        }
    }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static PromotionVideo Create(string title, string slug, bool isActive, DateTimeOffset? expiresAt)
    {
        return new PromotionVideo(title, slug, isActive, expiresAt);
    }

    public void SetDetails(string title, string slug, bool isActive, DateTimeOffset? expiresAt)
    {
        Title = CatalogRules.Required(title, 150, nameof(title));
        Slug = CatalogRules.Required(slug, 180, nameof(slug)).Trim().ToLowerInvariant();
        IsActive = isActive;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateDetails(string title, string slug, bool isActive, DateTimeOffset? expiresAt)
    {
        Title = CatalogRules.Required(title, 150, nameof(title));
        Slug = CatalogRules.Required(slug, 180, nameof(slug)).Trim().ToLowerInvariant();
        IsActive = isActive;
        ExpiresAt = expiresAt;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetVideo(string videoUrl, string contentType, string fileName, long fileSize)
    {
        VideoUrl = CatalogRules.Required(videoUrl, 2048, nameof(videoUrl));
        VideoContentType = CatalogRules.Required(contentType, 100, nameof(contentType));
        VideoFileName = CatalogRules.Required(fileName, 255, nameof(fileName));
        VideoFileSize = fileSize;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
