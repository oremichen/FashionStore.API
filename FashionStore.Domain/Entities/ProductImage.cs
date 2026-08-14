namespace FashionStore.Domain.Entities;
public sealed class ProductImage
{
    private ProductImage() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string? SmallUrl { get; private set; }
    public string? MediumUrl { get; private set; }
    public string? BigUrl { get; private set; }
    public byte[]? SmallImageData { get; private set; }
    public byte[]? MediumImageData { get; private set; }
    public byte[]? ImageData { get; private set; }
    public string? ImageContentType { get; private set; }
    public string? ImageFileName { get; private set; }
    public string? AlternativeText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static ProductImage Create(byte[] smallData, byte[] mediumData, byte[] largeData, string fileName, string? alternativeText,
        int sortOrder, bool isPrimary)
    {
        string[] allowed = ["image/webp"];
        var small = ImageRules.Validate(smallData, "image/webp", fileName, allowed);
        var medium = ImageRules.Validate(mediumData, "image/webp", fileName, allowed);
        var large = ImageRules.Validate(largeData, "image/webp", fileName, allowed);
        return new ProductImage
        {
            SmallImageData = small.Data,
            MediumImageData = medium.Data,
            ImageData = large.Data,
            ImageContentType = large.ContentType,
            ImageFileName = large.FileName,
            AlternativeText = CatalogRules.Optional(alternativeText, 250, nameof(alternativeText)),
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }
}
