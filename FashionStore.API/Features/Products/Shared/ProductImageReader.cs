namespace FashionStore.API.Features.Products.Shared;

public static class ProductImageReader
{
    public static async Task<IReadOnlyList<ProductImageRequest>> ReadAsync(
        IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var images = new List<ProductImageRequest>();
        foreach (var file in files)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            images.Add(new ProductImageRequest(stream.ToArray(), file.ContentType, file.FileName));
        }
        return images;
    }
}
