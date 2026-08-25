using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Products;

public class ProductFilter
{
    public string? Search { get; init; }
    public string? CategoryId { get; init; }
    public string? BrandId { get; init; }
    public string? Status { get; init; }
    public string? StockStatus { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string Sort { get; init; } = "newest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public int LowStockThreshold { get; init; } = 5;
}

public class StorefrontProductFilter
{
    public string? Search { get; init; }
    public string? CategorySlug { get; init; }
    public bool IncludeDescendants { get; init; }
    public string? BrandId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Colors { get; init; }
    public string? Sizes { get; init; }
    public bool? InStock { get; init; }
    public decimal? MinRating { get; init; }
    public string Sort { get; init; } = "newest";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
}

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAsync(ProductFilter query, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetStorefrontAsync(StorefrontProductFilter query, string? collection, string? excludingProductId, CancellationToken cancellationToken);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Product?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<bool> CategoryExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> BrandExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, string? excludingId, CancellationToken cancellationToken);
    Task<bool> SizeIdsExistAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken);
    Task<bool> ColorIdsExistAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken);
    Task SetSizesAndColorsAsync(string productId, IReadOnlyCollection<string> sizeIds, IReadOnlyCollection<string> colorIds, CancellationToken cancellationToken);
    Task SetVariantsAsync(string productId, IReadOnlyCollection<(string? SizeId, string? ColorId, decimal Price, int Quantity)> variants, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductVariant>> GetVariantsAsync(string productId, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(Product product, CancellationToken cancellationToken);
    Task<ProductImage?> GetImageAsync(string productId, string imageId, CancellationToken cancellationToken);
    Task<int> GetImageCountAsync(string productId, CancellationToken cancellationToken);
    Task DeleteImageAsync(ProductImage image, CancellationToken cancellationToken);
}
