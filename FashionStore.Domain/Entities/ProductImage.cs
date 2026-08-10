namespace FashionStore.Domain.Entities;
public sealed class ProductImage
{
    private ProductImage() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string? SmallUrl { get; private set; }
    public string? MediumUrl { get; private set; }
    public string BigUrl { get; private set; } = null!;
    public string? AlternativeText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
}
