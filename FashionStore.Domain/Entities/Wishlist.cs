namespace FashionStore.Domain.Entities
{
    public sealed class Wishlist
    {
        private readonly List<WishlistItem> _items = [];

        private Wishlist()
        {
        }

        public string Id { get; private set; } = null!;
        public string UserId { get; private set; } = null!;
        public ApplicationUser User { get; private set; } = null!;
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public IReadOnlyCollection<WishlistItem> Items
        {
            get
            {
                return _items;
            }
        }

        public static Wishlist Create(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            return new Wishlist
            {
                UserId = userId.Trim()
            };
        }

        public void AddItem(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                throw new ArgumentException("Product id is required.", nameof(productId));
            }

            var normalizedProductId = productId.Trim();
            if (_items.Any(item => item.ProductId == normalizedProductId))
            {
                return;
            }

            _items.Add(WishlistItem.Create(normalizedProductId));
            Touch();
        }

        public void RemoveItem(string wishlistItemId)
        {
            if (string.IsNullOrWhiteSpace(wishlistItemId))
            {
                throw new ArgumentException("Wishlist item id is required.", nameof(wishlistItemId));
            }

            var item = _items.SingleOrDefault(current => current.Id == wishlistItemId);

            if (item == null)
            {
                return;
            }

            _items.Remove(item);
            Touch();
        }

        public void ClearItems()
        {
            if (_items.Count == 0)
            {
                return;
            }

            _items.Clear();
            Touch();
        }

        private void Touch()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
}
