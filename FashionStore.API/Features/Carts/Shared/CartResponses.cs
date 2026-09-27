namespace FashionStore.API.Features.Carts.Shared;

public sealed class CartResponse
{
    public required string Id { get; init; }
    public required IReadOnlyList<CartItemResponse> Items { get; init; }
    public int ItemCount { get; init; }
    public int TotalQuantity { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class CartItemResponse
{
    public required string Id { get; init; }
    public required string ProductId { get; init; }
    public string? VariantId { get; init; }
    public string? ColorId { get; init; }
    public int Quantity { get; init; }
}
