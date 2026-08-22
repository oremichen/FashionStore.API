using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;

namespace FashionStore.Application.Abstractions.Products;

public interface IProductService
{
    Task<ResponseResult<PagedResponse<ProductResponse>>> GetAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<ResponseResult<PagedResponse<ProductResponse>>> GetStorefrontAsync(StorefrontProductQuery query, CancellationToken cancellationToken);
    Task<ResponseResult<PagedResponse<ProductResponse>>> GetCollectionAsync(string collection, int page, int pageSize, CancellationToken cancellationToken);
    Task<ResponseResult<ProductDetailResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<ResponseResult<PagedResponse<ProductResponse>>> GetRelatedAsync(string productId, int page, int pageSize, CancellationToken cancellationToken);
    Task<ResponseResult<ProductDetailResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<ProductResponse>> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<ProductResponse>> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<IReadOnlyList<ProductImageResponse>>> GetImagesAsync(string productId, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteImageAsync(string productId, string imageId, CancellationToken cancellationToken);
}
