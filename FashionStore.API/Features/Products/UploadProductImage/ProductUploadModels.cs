using System.ComponentModel.DataAnnotations;
using FashionStore.API.Features.Products.Shared;

namespace FashionStore.API.Features.Products.UploadProductImage;

public sealed record StoredProductImageUpload(byte[] Data, string ContentType, string FileName);

public sealed class ProductImageUploadRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<string> ImageUploadIds { get; init; } = [];
}

public sealed class CreateProductJsonRequest : ProductWriteRequest
{
    public IReadOnlyList<string> ImageUploadIds { get; init; } = [];
}

public sealed class UpdateProductJsonRequest : ProductWriteRequest
{
    [Required]
    public string ProductId { get; init; } = string.Empty;

    public IReadOnlyList<string> ImageUploadIds { get; init; } = [];
}

public sealed record ProductImageUploadResponse(
    string UploadId,
    string FileName,
    string ContentType,
    long FileSize,
    DateTimeOffset ExpiresAt);

internal sealed record ProductUploadMetadata(string ContentType, string FileName);
