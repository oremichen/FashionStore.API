using System.Text.Json;
using FashionStore.API.Features.Products.Shared;
using FashionStore.Domain.Entities;
using StackExchange.Redis;

namespace FashionStore.API.Features.Products.UploadProductImage;

public sealed class ProductUploadService(IConnectionMultiplexer connection) : IProductUploadService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
        "image/avif"
    ];

    private static readonly TimeSpan UploadExpiration = TimeSpan.FromMinutes(30);
    private const long MaximumFileSize = ImageRules.MaximumFileSize;
    private const string KeyPrefix = "fashion-store:product-upload:";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDatabase database = connection.GetDatabase();

    public async Task<ProductImageUploadResponse> UploadAsync(
        Stream body,
        string contentType,
        string fileName,
        long? contentLength,
        CancellationToken cancellationToken)
    {
        if (contentLength is > MaximumFileSize)
        {
            throw new ArgumentException("Image cannot exceed 5 MB.", nameof(body));
        }

        var data = await ReadBodyAsync(body, cancellationToken);
        var validated = ImageRules.Validate(data, contentType, fileName, AllowedContentTypes);
        var uploadId = Guid.NewGuid().ToString("N");
        await StoreAsync(
            uploadId,
            new StoredProductImageUpload(validated.Data, validated.ContentType, validated.FileName),
            UploadExpiration,
            cancellationToken);

        return new ProductImageUploadResponse(
            uploadId,
            validated.FileName,
            validated.ContentType,
            validated.FileSize,
            DateTimeOffset.UtcNow.Add(UploadExpiration));
    }

    public async Task<IReadOnlyList<ProductImageRequest>> TakeImagesAsync(
        IReadOnlyList<string> uploadIds,
        CancellationToken cancellationToken)
    {
        var images = new List<ProductImageRequest>(uploadIds.Count);
        foreach (var uploadId in uploadIds)
        {
            if (string.IsNullOrWhiteSpace(uploadId) || uploadId.Length > 64)
            {
                throw new ArgumentException("One or more image upload IDs are invalid.", nameof(uploadIds));
            }

            var upload = await TakeAsync(uploadId, cancellationToken);
            if (upload is null)
            {
                throw new KeyNotFoundException($"Image upload '{uploadId}' was not found or has expired.");
            }

            images.Add(new ProductImageRequest(upload.Data, upload.ContentType, upload.FileName));
        }

        return images;
    }

    private async Task StoreAsync(
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

    private async Task<StoredProductImageUpload?> TakeAsync(
        string uploadId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadataValue = await database.StringGetAsync(MetadataKey(uploadId));
        var dataValue = await database.StringGetAsync(DataKey(uploadId));
        if (!metadataValue.HasValue || !dataValue.HasValue)
        {
            return null;
        }

        var metadata = JsonSerializer.Deserialize<ProductUploadMetadata>(
            (byte[])metadataValue!,
            JsonOptions);
        if (metadata is null)
        {
            return null;
        }

        await database.KeyDeleteAsync([MetadataKey(uploadId), DataKey(uploadId)]);
        return new StoredProductImageUpload(
            (byte[])dataValue!,
            metadata.ContentType,
            metadata.FileName);
    }

    private static async Task<byte[]> ReadBodyAsync(Stream body, CancellationToken cancellationToken)
    {
        await using var limited = new LimitedReadStream(body, MaximumFileSize);
        await using var memory = new MemoryStream();
        await limited.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
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
