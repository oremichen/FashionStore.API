namespace FashionStore.Domain.Entities;

public sealed class ProductColor
{
    private ProductColor() { }

    private ProductColor(string productId, string colorId)
    {
        ProductId = productId;
        ColorId = colorId;
    }

    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string ColorId { get; private set; } = null!;
    public Color Color { get; private set; } = null!;

    public static ProductColor Create(string productId, string colorId)
    {
        return new ProductColor(productId, colorId);
    }
}
