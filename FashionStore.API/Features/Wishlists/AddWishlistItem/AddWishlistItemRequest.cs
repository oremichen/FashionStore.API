using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Wishlists.AddWishlistItem;

public sealed class AddWishlistItemRequest
{
    [Required]
    [StringLength(50)]
    public string ProductId { get; init; } = string.Empty;
}
