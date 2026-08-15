using FashionStore.Application.Dtos.Request;

namespace FashionStore.Application.Abstractions.Products;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAsync(ProductQuery query, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetStorefrontAsync(StorefrontProductQuery query, string? collection, string? excludingProductId, CancellationToken cancellationToken);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> BrandExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, string? excludingId, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Product product, CancellationToken cancellationToken);
    Task<ProductImage?> GetImageAsync(string productId, string imageId, CancellationToken cancellationToken);
    Task DeleteImageAsync(ProductImage image, CancellationToken cancellationToken);
}
