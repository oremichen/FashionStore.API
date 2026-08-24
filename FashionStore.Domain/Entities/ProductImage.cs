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

    public static ProductImage Create(string smallUrl, string mediumUrl, string bigUrl, string? contentType, string? fileName, string? alternativeText,
        int sortOrder, bool isPrimary)
    {
        return new ProductImage
        {
            SmallUrl = Rules.Required(smallUrl, 2048, nameof(smallUrl)),
            MediumUrl = Rules.Required(mediumUrl, 2048, nameof(mediumUrl)),
            BigUrl = Rules.Required(bigUrl, 2048, nameof(bigUrl)),
            ImageContentType = contentType,
            ImageFileName = fileName,
            AlternativeText = Rules.Optional(alternativeText, 250, nameof(alternativeText)),
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }
}
