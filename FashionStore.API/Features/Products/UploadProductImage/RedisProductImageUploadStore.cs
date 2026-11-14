using System.Text.Json;
using StackExchange.Redis;

namespace FashionStore.API.Features.Products.UploadProductImage;

internal sealed class RedisProductImageUploadStore(IConnectionMultiplexer connection) : IProductImageUploadStore
{
    private const string KeyPrefix = "fashion-store:product-upload:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = connection.GetDatabase();

    public async Task StoreAsync(
        string uploadId,
        StoredProductImageUpload upload,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadata = JsonSerializer.SerializeToUtf8Bytes(
            new ProductUploadMetadata(upload.ContentType, upload.FileName),
            JsonOptions);
        var transaction = database.CreateTransaction();
        _ = transaction.StringSetAsync(MetadataKey(uploadId), metadata, expiration);
        _ = transaction.StringSetAsync(DataKey(uploadId), upload.Data, expiration);
        if (!await transaction.ExecuteAsync())
        {
            throw new InvalidOperationException("The product image upload could not be stored.");
        }
    }

    public async Task<StoredProductImageUpload?> TakeAsync(string uploadId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadataValue = await database.StringGetAsync(MetadataKey(uploadId));
        var dataValue = await database.StringGetAsync(DataKey(uploadId));
        if (!metadataValue.HasValue || !dataValue.HasValue)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<ProductUploadMetadata>((byte[])metadataValue!, JsonOptions);
        if (metadata is null)
        {
            return null;
        }

        await database.KeyDeleteAsync([MetadataKey(uploadId), DataKey(uploadId)]);
        return new StoredProductImageUpload((byte[])dataValue!, metadata.ContentType, metadata.FileName);
    }

    private static RedisKey MetadataKey(string uploadId)
    {
        return $"{KeyPrefix}{uploadId}:metadata";
    }

    private static RedisKey DataKey(string uploadId)
    {
        return $"{KeyPrefix}{uploadId}:data";
    }
}
