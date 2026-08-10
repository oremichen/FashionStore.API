namespace FashionStore.Domain.Entities;
public sealed class ProductTag
{
    private ProductTag() { }
    public string Id { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static ProductTag Create(string name, string slug)
    {
        return new ProductTag { Name = CatalogRules.Required(name, 100, nameof(name)), Slug = CatalogRules.Slug(slug, 120) };
    }
}
