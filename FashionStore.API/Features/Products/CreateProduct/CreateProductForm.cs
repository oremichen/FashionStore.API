namespace FashionStore.API.Features.Products.CreateProduct;

public sealed class CreateProductForm : ProductWriteRequest
{
    public List<IFormFile> Images { get; init; } = [];
}
