using Microsoft.Extensions.Caching.Memory;

namespace FashionStore.API.Features.Products.UploadProductImage;

internal sealed class MemoryProductImageUploadStore(IMemoryCache cache) : IProductImageUploadStore
{
    private const string KeyPrefix = "fashion-store:product-upload:";

    public Task StoreAsync(
        string uploadId,
        StoredProductImageUpload upload,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.Set(Key(uploadId), upload, expiration);
        return Task.CompletedTask;
    }

    public Task<StoredProductImageUpload?> TakeAsync(string uploadId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        cache.TryGetValue(Key(uploadId), out StoredProductImageUpload? upload);
        cache.Remove(Key(uploadId));
        return Task.FromResult(upload);
    }

    private static string Key(string uploadId)
    {
        return $"{KeyPrefix}{uploadId}";
    }
}
