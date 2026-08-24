namespace FashionStore.API.Features.Products.CreateProduct;

public sealed class CreateProductRequest : ProductWriteRequest
{
    public required IReadOnlyList<ProductImageRequest> ImageRequests { get; init; }
}
