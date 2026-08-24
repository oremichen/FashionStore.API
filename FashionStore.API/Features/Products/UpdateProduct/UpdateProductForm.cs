using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Products.UpdateProduct;

public sealed class UpdateProductForm : ProductWriteRequest
{
    [Required] public string ProductId { get; init; } = string.Empty;
    public List<IFormFile> Images { get; init; } = [];
}
