namespace FashionStore.Domain.Entities;

public sealed class ProductVariantImage
{
    private ProductVariantImage() { }

    public string Id { get; private set; } = null!;
    public string ProductVariantId { get; private set; } = null!;
    public ProductVariant ProductVariant { get; private set; } = null!;
    public string ProductImageId { get; private set; } = null!;
    public ProductImage ProductImage { get; private set; } = null!;
    public int SortOrder { get; private set; }
}
