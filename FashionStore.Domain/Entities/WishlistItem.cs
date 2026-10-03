namespace FashionStore.Domain.Entities;

public sealed class WishlistItem
{
    private WishlistItem()
    {
    }

    public string Id { get; private set; } = null!;
    public string WishlistId { get; private set; } = null!;
    public Wishlist Wishlist { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static WishlistItem Create(string productId)
    {
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id is required.", nameof(productId));
        }

        return new WishlistItem
        {
            ProductId = productId.Trim()
        };
    }
}
