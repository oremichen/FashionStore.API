namespace FashionStore.Domain.Entities;

public sealed class CartItem
{
    private CartItem()
    {
    }

    public string Id { get; private set; } = null!;
    public string CartId { get; private set; } = null!;
    public Cart Cart { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public string? VariantId { get; private set; }
    public string? ColorId { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static CartItem Create(string productId, string? variantId, string? colorId, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("Cart item quantity must be greater than zero.", nameof(quantity));
        }

        return new CartItem
        {
            ProductId = productId.Trim(),
            VariantId = Normalize(variantId),
            ColorId = Normalize(colorId),
            Quantity = quantity
        };
    }

    public void IncreaseQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Cart item quantity must be greater than zero.", nameof(quantity));
        }

        Quantity = checked(Quantity + quantity);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentException("Cart item quantity must be greater than zero.", nameof(quantity));
        }

        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
