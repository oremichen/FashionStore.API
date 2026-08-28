namespace FashionStore.Domain.Entities;

public sealed class Product
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductReview> _reviews = [];
    private readonly List<ProductSize> _productSizes = [];
    private readonly List<ProductColor> _productColors = [];
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
    public decimal? MinPrice { get; private set; }
    public decimal? MaxPrice { get; private set; }
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
    public IReadOnlyCollection<ProductSize> ProductSizes { get { return _productSizes; } }
    public IReadOnlyCollection<ProductColor> ProductColors { get { return _productColors; } }

    public static Product Create(string categoryId, string? brandId, string name, string slug, decimal newPrice, string currencyCode, int stock)
    {
        Rules.NonNegative(newPrice, nameof(newPrice));
        Rules.NonNegative(stock, nameof(stock));
        var currency = Rules.Required(currencyCode, 3, nameof(currencyCode)).ToUpperInvariant();
        if (currency.Length != 3) throw new ArgumentException("Currency code must contain exactly three letters.");
        return new Product { CategoryId = Rules.Required(categoryId, 50, nameof(categoryId)), BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId.Trim(), Name = Rules.Required(name, 250, nameof(name)), Slug = Rules.Slug(slug, 280), NewPrice = newPrice, CurrencyCode = currency, AvailabilityCount = stock };
    }

    public void Update(string categoryId, string? brandId, string name, string slug, string? description,
        string? additionalInformation, string? shortDescription, decimal? oldPrice, decimal newPrice, string currencyCode, int stock,
        decimal? weight, string? weightUnit, bool isFeatured, bool isNewArrival, bool isMinMaxPrice = false,
        decimal? minPrice = null, decimal? maxPrice = null)
    {
        Rules.NonNegative(newPrice, nameof(newPrice));
        Rules.NonNegative(stock, nameof(stock));
        if (oldPrice.HasValue) Rules.NonNegative(oldPrice.Value, nameof(oldPrice));
        if (weight.HasValue) Rules.NonNegative(weight.Value, nameof(weight));
        if (isMinMaxPrice)
        {
            if (!minPrice.HasValue || !maxPrice.HasValue) throw new ArgumentException("MinPrice and MaxPrice are required when IsMinMaxPrice is true.");
            Rules.NonNegative(minPrice.Value, nameof(minPrice));
            Rules.NonNegative(maxPrice.Value, nameof(maxPrice));
            if (minPrice.Value > maxPrice.Value) throw new ArgumentException("MinPrice cannot be greater than MaxPrice.");
            newPrice = 0;
        }
        CategoryId = Rules.Required(categoryId, 50, nameof(categoryId));
        BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId.Trim();
        Name = Rules.Required(name, 250, nameof(name));
        Slug = Rules.Slug(slug, 280);
        Description = Rules.Optional(description, 10000, nameof(description));
        AdditionalInformation = Rules.Optional(additionalInformation, 10000, nameof(additionalInformation));
        ShortDescription = Rules.Optional(shortDescription, 500, nameof(shortDescription));
        OldPrice = oldPrice;
        NewPrice = newPrice;
        MinPrice = isMinMaxPrice ? minPrice : null;
        MaxPrice = isMinMaxPrice ? maxPrice : null;
        Discount = oldPrice > newPrice && oldPrice > 0 ? decimal.Round((oldPrice.Value - newPrice) / oldPrice.Value * 100, 2) : null;
        var currency = Rules.Required(currencyCode, 3, nameof(currencyCode)).ToUpperInvariant();
        if (currency.Length != 3) throw new ArgumentException("Currency code must contain exactly three letters.");
        CurrencyCode = currency;
        AvailabilityCount = stock;
        Weight = weight;
        WeightUnit = Rules.Optional(weightUnit, 20, nameof(weightUnit));
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

    public void AddImages(IEnumerable<(string SmallUrl, string MediumUrl, string BigUrl, string ContentType, string FileName)> images)
    {
        var sortOrder = _images.Count == 0 ? 0 : _images.Max(x => x.SortOrder) + 1;
        var hasPrimary = _images.Any(x => x.IsPrimary);
        foreach (var image in images)
        {
            _images.Add(ProductImage.Create(image.SmallUrl, image.MediumUrl, image.BigUrl, image.ContentType, image.FileName, Name, sortOrder++, !hasPrimary));
            hasPrimary = true;
        }
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
