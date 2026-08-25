namespace FashionStore.Domain.Entities;
public sealed class ProductVariant
{
    private ProductVariant() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string? SizeId { get; private set; }
    public Size? Size { get; private set; }
    public string? ColorId { get; private set; }
    public Color? Color { get; private set; }
    public string Sku { get; private set; } = null!;
    public string? Barcode { get; private set; }
    public decimal? OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public decimal? CostPrice { get; private set; }
    public decimal? Discount { get; private set; }
    public int AvailabilityCount { get; private set; }
    public int LowStockThreshold { get; private set; }
    public decimal? Weight { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static ProductVariant Create(string productId, string? sizeId, string? colorId, decimal price, int quantity)
    {
        Rules.NonNegative(price, nameof(price));
        Rules.NonNegative(quantity, nameof(quantity));
        if (string.IsNullOrWhiteSpace(sizeId) && string.IsNullOrWhiteSpace(colorId))
            throw new ArgumentException("A product variant must have a size or color.");
        return new ProductVariant
        {
            ProductId = Rules.Required(productId, 50, nameof(productId)),
            SizeId = string.IsNullOrWhiteSpace(sizeId) ? null : sizeId.Trim(),
            ColorId = string.IsNullOrWhiteSpace(colorId) ? null : colorId.Trim(),
            Sku = $"VAR-{Guid.NewGuid():N}", NewPrice = price, AvailabilityCount = quantity
        };
    }
}
