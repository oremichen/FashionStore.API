namespace FashionStore.Domain.Entities;

public sealed class Product
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductReview> _reviews = [];
    private Product() { }
    public string Id { get; private set; } = null!;
    public string CategoryId { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public string? BrandId { get; private set; }
    public Brand? Brand { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string? AdditionalInformation { get; private set; }
    public string? ShortDescription { get; private set; }
    public decimal? OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public decimal? Discount { get; private set; }
    public string CurrencyCode { get; private set; } = "NGN";
    public int AvailabilityCount { get; private set; }
    public decimal? Weight { get; private set; }
    public string? WeightUnit { get; private set; } = "g";
    public int RatingsCount { get; private set; }
    public decimal RatingsValue { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsNewArrival { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsArchived { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public IReadOnlyCollection<ProductImage> Images { get { return _images; } }
    public IReadOnlyCollection<ProductVariant> Variants { get { return _variants; } }
    public IReadOnlyCollection<ProductReview> Reviews { get { return _reviews; } }

    public static Product Create(string categoryId, string? brandId, string name, string slug, decimal newPrice, string currencyCode, int stock)
    {
        CatalogRules.NonNegative(newPrice, nameof(newPrice));
        CatalogRules.NonNegative(stock, nameof(stock));
        var currency = CatalogRules.Required(currencyCode, 3, nameof(currencyCode)).ToUpperInvariant();
        if (currency.Length != 3) throw new ArgumentException("Currency code must contain exactly three letters.");
        return new Product { CategoryId = CatalogRules.Required(categoryId, 50, nameof(categoryId)), BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId.Trim(), Name = CatalogRules.Required(name, 250, nameof(name)), Slug = CatalogRules.Slug(slug, 280), NewPrice = newPrice, CurrencyCode = currency, AvailabilityCount = stock };
    }

    public void Update(string categoryId, string? brandId, string name, string slug, string? description,
        string? additionalInformation, string? shortDescription, decimal? oldPrice, decimal newPrice, string currencyCode, int stock,
        decimal? weight, string? weightUnit, bool isFeatured, bool isNewArrival)
    {
        CatalogRules.NonNegative(newPrice, nameof(newPrice));
        CatalogRules.NonNegative(stock, nameof(stock));
        if (oldPrice.HasValue) CatalogRules.NonNegative(oldPrice.Value, nameof(oldPrice));
        if (weight.HasValue) CatalogRules.NonNegative(weight.Value, nameof(weight));
        CategoryId = CatalogRules.Required(categoryId, 50, nameof(categoryId));
        BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId.Trim();
        Name = CatalogRules.Required(name, 250, nameof(name));
        Slug = CatalogRules.Slug(slug, 280);
        Description = CatalogRules.Optional(description, 10000, nameof(description));
        AdditionalInformation = CatalogRules.Optional(additionalInformation, 10000, nameof(additionalInformation));
        ShortDescription = CatalogRules.Optional(shortDescription, 500, nameof(shortDescription));
        OldPrice = oldPrice;
        NewPrice = newPrice;
        Discount = oldPrice > newPrice && oldPrice > 0 ? decimal.Round((oldPrice.Value - newPrice) / oldPrice.Value * 100, 2) : null;
        var currency = CatalogRules.Required(currencyCode, 3, nameof(currencyCode)).ToUpperInvariant();
        if (currency.Length != 3) throw new ArgumentException("Currency code must contain exactly three letters.");
        CurrencyCode = currency;
        AvailabilityCount = stock;
        Weight = weight;
        WeightUnit = CatalogRules.Optional(weightUnit, 20, nameof(weightUnit));
        IsFeatured = isFeatured;
        IsNewArrival = isNewArrival;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(string status)
    {
        switch (status.Trim().ToLowerInvariant())
        {
            case "draft": IsArchived = false; IsActive = false; PublishedAt = null; break;
            case "active": IsArchived = false; IsActive = true; PublishedAt ??= DateTimeOffset.UtcNow; break;
            case "inactive": IsArchived = false; IsActive = false; PublishedAt ??= DateTimeOffset.UtcNow; break;
            case "archived": IsArchived = true; IsActive = false; break;
            default: throw new ArgumentException("Status must be draft, active, inactive, or archived.", nameof(status));
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddImages(IEnumerable<(byte[] SmallData, byte[] MediumData, byte[] LargeData, string FileName)> images)
    {
        var sortOrder = _images.Count == 0 ? 0 : _images.Max(x => x.SortOrder) + 1;
        var hasPrimary = _images.Any(x => x.IsPrimary);
        foreach (var image in images)
        {
            _images.Add(ProductImage.Create(image.SmallData, image.MediumData, image.LargeData, image.FileName, Name, sortOrder++, !hasPrimary));
            hasPrimary = true;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
