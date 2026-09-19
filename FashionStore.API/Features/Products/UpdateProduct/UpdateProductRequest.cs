namespace FashionStore.API.Features.Products.UpdateProduct;

public sealed class UpdateProductRequest : ProductWriteRequest
{
    public string ProductId { get; init; } = string.Empty;
    public required IReadOnlyList<ProductImageRequest> ImageRequests { get; init; }
}
