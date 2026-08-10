namespace FashionStore.Domain.Entities;
public sealed class ProductTagMapping
{
    private ProductTagMapping() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string ProductTagId { get; private set; } = null!;
    public ProductTag ProductTag { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
}
