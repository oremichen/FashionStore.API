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
    public string? ImageUrl { get; private set; }
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
        Name = Rules.Required(name, 150, nameof(name));
        Slug = Rules.Slug(slug, 180);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        WebsiteUrl = Rules.Optional(websiteUrl, 2048, nameof(websiteUrl));
        if (WebsiteUrl is not null && (!Uri.TryCreate(WebsiteUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")))
            throw new ArgumentException("Website URL must be an absolute HTTP or HTTPS URL.", nameof(websiteUrl));
        IsActive = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetImageUrl(string imageUrl, string? contentType = null, string? fileName = null)
    {
        ImageUrl = Rules.Required(imageUrl, 2048, nameof(imageUrl));
        ImageContentType = contentType;
        ImageFileName = fileName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
