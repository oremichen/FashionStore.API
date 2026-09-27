using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Carts.AddCartItem;

public sealed class AddCartItemRequest
{
    [Required, StringLength(50)] public string ProductId { get; init; } = string.Empty;
    [StringLength(50)] public string? VariantId { get; init; }
    [StringLength(50)] public string? ColorId { get; init; }
    [Range(1, int.MaxValue)] public int Quantity { get; init; }
}
