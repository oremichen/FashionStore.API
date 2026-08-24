using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Products.UpdateProduct;

public sealed class UpdateProductRequest : ProductWriteRequest
{
    [Required] public required string ProductId { get; init; }
    public required IReadOnlyList<ProductImageRequest> ImageRequests { get; init; }
}
