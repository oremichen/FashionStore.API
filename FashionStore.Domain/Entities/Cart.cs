namespace FashionStore.Domain.Entities
{
    public sealed class Cart
    {
        private readonly List<CartItem> _items = [];

        private Cart()
        {
        }

        public string Id { get; private set; } = null!;
        public string UserId { get; private set; } = null!;
        public ApplicationUser User { get; private set; } = null!;
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
        public IReadOnlyCollection<CartItem> Items
        {
            get
            {
                return _items;
            }
        }

        public static Cart Create(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("User id is required.", nameof(userId));
            }

            return new Cart
            {
                UserId = userId.Trim()
            };
        }

        public void AddItem(string productId, string? variantId, string? colorId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Cart item quantity must be greater than zero.", nameof(quantity));
            }

            var item = _items.SingleOrDefault(current =>
                current.ProductId == productId &&
                current.VariantId == Normalize(variantId) &&
                current.ColorId == Normalize(colorId));

            if (item is null)
            {
                _items.Add(CartItem.Create(productId, variantId, colorId, quantity));
            }
            else
            {
                item.IncreaseQuantity(quantity);
            }

            Touch();
        }

        public void UpdateItemQuantity(string cartItemId, int quantity)
        {
            var item = FindItem(cartItemId);
            item.SetQuantity(quantity);
            Touch();
        }

        public void RemoveItem(string cartItemId)
        {
            var item = FindItem(cartItemId);
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

        private CartItem FindItem(string cartItemId)
        {
            if (string.IsNullOrWhiteSpace(cartItemId))
            {
                throw new ArgumentException("Cart item id is required.", nameof(cartItemId));
            }

            return _items.SingleOrDefault(item => item.Id == cartItemId)
                ?? throw new InvalidOperationException("The cart item was not found.");
        }

        private void Touch()
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
