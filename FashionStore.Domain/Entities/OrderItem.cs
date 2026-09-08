namespace FashionStore.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem() { }
    public string Id { get; private set; } = null!;
    public string OrderId { get; private set; } = null!;
    public Order Order { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public string? VariantId { get; private set; }
    public string? ColorId { get; private set; }
    public string? ColorName { get; private set; }
    public string? SizeName { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderItem Create(string productId, string? variantId, string? colorId, string? colorName, string? sizeName, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Order item quantity must be greater than zero.");
        if (unitPrice < 0) throw new ArgumentException("Order item price cannot be negative.");
        return new OrderItem
        {
            ProductId = productId,
            VariantId = variantId,
            ColorId = string.IsNullOrWhiteSpace(colorId) ? null : colorId.Trim(),
            ColorName = string.IsNullOrWhiteSpace(colorName) ? null : colorName.Trim(),
            SizeName = string.IsNullOrWhiteSpace(sizeName) ? null : sizeName.Trim(),
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = unitPrice * quantity
        };
    }
}
