namespace FashionStore.Domain.Entities;

public sealed class OrderItem
{
    private OrderItem() { }
    public string Id { get; private set; } = null!;
    public string OrderId { get; private set; } = null!;
    public Order Order { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public string? VariantId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }

    public static OrderItem Create(string productId, string? variantId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Order item quantity must be greater than zero.");
        if (unitPrice < 0) throw new ArgumentException("Order item price cannot be negative.");
        return new OrderItem
        {
            ProductId = productId, VariantId = variantId, ProductName = productName,
            UnitPrice = unitPrice, Quantity = quantity, LineTotal = unitPrice * quantity
        };
    }
}
