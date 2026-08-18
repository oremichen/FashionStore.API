namespace FashionStore.Domain.Entities;

public sealed class ProductSize
{
    private ProductSize() { }

    private ProductSize(string productId, string sizeId)
    {
        ProductId = productId;
        SizeId = sizeId;
    }

    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string SizeId { get; private set; } = null!;
    public Size Size { get; private set; } = null!;

    public static ProductSize Create(string productId, string sizeId)
    {
        return new ProductSize(productId, sizeId);
    }
}
