namespace FashionStore.API.Features.Products.UploadProductImage;

public interface IProductImageUploadStore
{
    Task StoreAsync(string uploadId, StoredProductImageUpload upload, TimeSpan expiration, CancellationToken cancellationToken);
    Task<StoredProductImageUpload?> TakeAsync(string uploadId, CancellationToken cancellationToken);
}
