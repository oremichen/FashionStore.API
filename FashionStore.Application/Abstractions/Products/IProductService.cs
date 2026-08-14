using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;

namespace FashionStore.Application.Abstractions.Products;

public interface IProductService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> GetAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ResponseResult<ProductResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<ProductResponse>> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> GetImagesAsync(string productId, CancellationToken cancellationToken);
    Task<ProductImageFileResponse?> GetImageAsync(string productId, string imageId, string size, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteImageAsync(string productId, string imageId, CancellationToken cancellationToken);
}
