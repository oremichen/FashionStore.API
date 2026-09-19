using FashionStore.API.Features.Products.Shared;

namespace FashionStore.API.Features.Products.UploadProductImage;

public interface IProductUploadService
{
    Task<ProductImageUploadResponse> UploadAsync(
        Stream body,
        string contentType,
        string fileName,
        long? contentLength,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductImageRequest>> TakeImagesAsync(
        IReadOnlyList<string> uploadIds,
        CancellationToken cancellationToken);
}
