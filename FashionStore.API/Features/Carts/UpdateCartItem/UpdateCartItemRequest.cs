using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Carts.UpdateCartItem;

public sealed class UpdateCartItemRequest
{
    [Range(1, int.MaxValue)] public int Quantity { get; init; }
}
