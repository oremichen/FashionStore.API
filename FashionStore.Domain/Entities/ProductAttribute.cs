namespace FashionStore.Domain.Entities;
public sealed class ProductAttribute
{
    private ProductAttribute() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Value { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsFilterable { get; private set; }
}
