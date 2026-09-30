using System.Text.Json;
using FashionStore.API.Features.Products.Shared;
using FashionStore.Domain.Entities;
using StackExchange.Redis;

namespace FashionStore.API.Features.Products.UploadProductImage;

public sealed class ProductUploadService(
    IConnectionMultiplexer connection,
    ILogger<ProductUploadService> logger) : IProductUploadService
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
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        logger.LogInformation(
            "Product upload service started. FileName: {FileName}, ContentType: {ContentType}, DeclaredLength: {ContentLength}, BodyType: {BodyType}, CanRead: {CanRead}, CanSeek: {CanSeek}",
            fileName,
            contentType,
            contentLength,
            body.GetType().FullName,
            body.CanRead,
            body.CanSeek);

        if (contentLength is > MaximumFileSize)
        {
            logger.LogError(
                "Product upload rejected before reading body because declared length exceeds limit. FileName: {FileName}, DeclaredLength: {ContentLength}, MaximumLength: {MaximumLength}",
                fileName,
                contentLength,
                MaximumFileSize);
            throw new ArgumentException("Image cannot exceed 5 MB.", nameof(body));
        }

        var data = await ReadBodyAsync(body, cancellationToken);
        logger.LogInformation(
            "Product upload request body read. FileName: {FileName}, BytesRead: {BytesRead}, ContentType: {ContentType}, ElapsedMilliseconds: {ElapsedMilliseconds}",
            fileName,
            data.Length,
            contentType,
            stopwatch.ElapsedMilliseconds);

        var validated = ImageRules.Validate(data, contentType, fileName, AllowedContentTypes);
        logger.LogInformation(
            "Product upload validation completed. FileName: {FileName}, ValidatedFileName: {ValidatedFileName}, ContentType: {ContentType}, FileSize: {FileSize}, ElapsedMilliseconds: {ElapsedMilliseconds}",
            fileName,
            validated.FileName,
            validated.ContentType,
            validated.FileSize,
            stopwatch.ElapsedMilliseconds);

        var uploadId = Guid.NewGuid().ToString("N");
        logger.LogInformation(
            "Product upload storage started. UploadId: {UploadId}, FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}, ExpirationMinutes: {ExpirationMinutes}",
            uploadId,
            validated.FileName,
            validated.ContentType,
            validated.FileSize,
            UploadExpiration.TotalMinutes);

        await StoreAsync(
            uploadId,
            new StoredProductImageUpload(validated.Data, validated.ContentType, validated.FileName),
            UploadExpiration,
            cancellationToken);

        logger.LogInformation(
            "Product upload service completed. UploadId: {UploadId}, FileName: {FileName}, FileSize: {FileSize}, ElapsedMilliseconds: {ElapsedMilliseconds}",
            uploadId,
            validated.FileName,
            validated.FileSize,
            stopwatch.ElapsedMilliseconds);

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
        logger.LogInformation("Taking product image uploads. UploadCount: {UploadCount}", uploadIds.Count);
        var images = new List<ProductImageRequest>(uploadIds.Count);
        foreach (var uploadId in uploadIds)
        {
            logger.LogInformation("Taking product image upload started. UploadId: {UploadId}", uploadId);
            if (string.IsNullOrWhiteSpace(uploadId) || uploadId.Length > 64)
            {
                logger.LogError("Product image upload ID rejected. UploadId: {UploadId}", uploadId);
                throw new ArgumentException("One or more image upload IDs are invalid.", nameof(uploadIds));
            }

            var upload = await TakeAsync(uploadId, cancellationToken);
            if (upload is null)
            {
                logger.LogError("Product image upload not found or expired. UploadId: {UploadId}", uploadId);
                throw new KeyNotFoundException($"Image upload '{uploadId}' was not found or has expired.");
            }

            images.Add(new ProductImageRequest(upload.Data, upload.ContentType, upload.FileName));
            logger.LogInformation(
                "Product image upload taken successfully. UploadId: {UploadId}, FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}",
                uploadId,
                upload.FileName,
                upload.ContentType,
                upload.Data.Length);
        }

        logger.LogInformation("Product image uploads taken successfully. UploadCount: {UploadCount}", images.Count);
        return images;
    }

    private async Task StoreAsync(
        string uploadId,
        StoredProductImageUpload upload,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation(
            "Writing product image upload to Redis. UploadId: {UploadId}, FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}, ExpirationSeconds: {ExpirationSeconds}",
            uploadId,
            upload.FileName,
            upload.ContentType,
            upload.Data.Length,
            expiration.TotalSeconds);

        var metadata = JsonSerializer.SerializeToUtf8Bytes(
            new ProductUploadMetadata(upload.ContentType, upload.FileName),
            JsonOptions);
        var transaction = database.CreateTransaction();
        _ = transaction.StringSetAsync(MetadataKey(uploadId), metadata, expiration);
        _ = transaction.StringSetAsync(DataKey(uploadId), upload.Data, expiration);
        if (!await transaction.ExecuteAsync())
        {
            logger.LogError("Redis transaction failed while storing product image upload. UploadId: {UploadId}", uploadId);
            throw new InvalidOperationException("The product image upload could not be stored.");
        }

        logger.LogInformation("Product image upload stored in Redis successfully. UploadId: {UploadId}", uploadId);
    }

    private async Task<StoredProductImageUpload?> TakeAsync(
        string uploadId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        logger.LogInformation("Reading product image upload from Redis. UploadId: {UploadId}", uploadId);
        var metadataValue = await database.StringGetAsync(MetadataKey(uploadId));
        var dataValue = await database.StringGetAsync(DataKey(uploadId));
        if (!metadataValue.HasValue || !dataValue.HasValue)
        {
            logger.LogError(
                "Product image upload Redis entry missing. UploadId: {UploadId}, MetadataFound: {MetadataFound}, DataFound: {DataFound}",
                uploadId,
                metadataValue.HasValue,
                dataValue.HasValue);
            return null;
        }

        var metadata = JsonSerializer.Deserialize<ProductUploadMetadata>(
            (byte[])metadataValue!,
            JsonOptions);
        if (metadata is null)
        {
            logger.LogError("Product image upload metadata could not be deserialized. UploadId: {UploadId}", uploadId);
            return null;
        }

        await database.KeyDeleteAsync([MetadataKey(uploadId), DataKey(uploadId)]);
        logger.LogInformation(
            "Product image upload read and removed from Redis successfully. UploadId: {UploadId}, FileName: {FileName}, ContentType: {ContentType}, FileSize: {FileSize}",
            uploadId,
            metadata.FileName,
            metadata.ContentType,
            dataValue!.Length());
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
