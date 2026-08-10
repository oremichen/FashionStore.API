namespace FashionStore.Domain.Entities;

public sealed class Brand
{
    private readonly List<Product> _products = [];
    private Brand() { }
    private Brand(string name, string slug, string? description, string? websiteUrl, bool isActive)
    {
        SetDetails(name, slug, description, websiteUrl, isActive);
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? WebsiteUrl { get; private set; }
    public byte[]? ImageData { get; private set; }
    public string? ImageContentType { get; private set; }
    public string? ImageFileName { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<Product> Products { get { return _products; } }

    public static Brand Create(string name, string slug, string? description, string? websiteUrl, bool isActive = true)
    {
        return new Brand(name, slug, description, websiteUrl, isActive);
    }

    public void SetDetails(string name, string slug, string? description, string? websiteUrl, bool isActive)
    {
        Name = CatalogRules.Required(name, 150, nameof(name));
        Slug = CatalogRules.Slug(slug, 180);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        WebsiteUrl = CatalogRules.Optional(websiteUrl, 2048, nameof(websiteUrl));
        if (WebsiteUrl is not null && (!Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            throw new ArgumentException("Website URL must be an absolute HTTP or HTTPS URL.", nameof(websiteUrl));
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImage(byte[] data, string contentType, string fileName)
    {
        if (data is null || data.Length == 0) throw new ArgumentException("Image cannot be empty.", nameof(data));
        if (data.Length > 5 * 1024 * 1024) throw new ArgumentException("Image cannot exceed 5 MB.", nameof(data));
        string[] allowed = ["image/jpeg", "image/png", "image/webp", "image/gif"];
        if (!allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Only JPEG, PNG, WebP, and GIF images are supported.", nameof(contentType));
        ImageData = data.ToArray();
        ImageContentType = contentType.ToLowerInvariant();
        ImageFileName = CatalogRules.Required(Path.GetFileName(fileName), 255, nameof(fileName));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
