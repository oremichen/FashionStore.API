using FashionStore.Application.Abstractions.Products;
using FashionStore.Application.Dtos.Request;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.ProductRepo;

public sealed class ProductRepository(FashionStoreDbContext dbContext) : IProductRepository
{
    private IQueryable<Product> StorefrontProducts() => dbContext.Products.AsNoTracking()
        .Include(x => x.Category).Include(x => x.Brand).Include(x => x.Images)
        .Where(x => !x.IsArchived && x.IsActive && x.PublishedAt != null);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetStorefrontAsync(
        StorefrontProductQuery request, string? collection, string? excludingProductId, CancellationToken ct)
    {
        var query = StorefrontProducts();
        if (!string.IsNullOrWhiteSpace(excludingProductId)) query = query.Where(x => x.Id != excludingProductId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Slug.ToLower().Contains(term) ||
                (x.ShortDescription != null && x.ShortDescription.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(request.CategorySlug) && collection != "related")
        {
            var slug = request.CategorySlug.Trim().ToLower();
            if (request.IncludeDescendants)
            {
                var categoryId = await dbContext.Categories.AsNoTracking()
                    .Where(x => x.DeletedAt == null && x.Slug.ToLower() == slug).Select(x => x.Id).SingleOrDefaultAsync(ct);
                if (categoryId is null) query = query.Where(_ => false);
                else
                {
                    var categoryIds = new List<string> { categoryId };
                    var level = new List<string> { categoryId };
                    while (level.Count > 0)
                    {
                        level = await dbContext.Categories.AsNoTracking()
                            .Where(x => x.DeletedAt == null && x.ParentId != null && level.Contains(x.ParentId))
                            .Select(x => x.Id).ToListAsync(ct);
                        categoryIds.AddRange(level);
                    }
                    query = query.Where(x => categoryIds.Contains(x.CategoryId));
                }
            }
            else query = query.Where(x => x.Category.Slug.ToLower() == slug);
        }
        if (!string.IsNullOrWhiteSpace(request.BrandId)) query = query.Where(x => x.BrandId == request.BrandId);
        if (request.MinPrice.HasValue) query = query.Where(x => x.NewPrice >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) query = query.Where(x => x.NewPrice <= request.MaxPrice.Value);
        if (request.InStock.HasValue) query = request.InStock.Value
            ? query.Where(x => x.AvailabilityCount > 0 || x.Variants.Any(v => v.IsActive && v.AvailabilityCount > 0))
            : query.Where(x => x.AvailabilityCount == 0 && !x.Variants.Any(v => v.IsActive && v.AvailabilityCount > 0));
        if (request.MinRating.HasValue) query = query.Where(x => x.RatingsValue >= request.MinRating.Value);
        var colors = Split(request.Colors);
        if (colors.Length > 0) query = query.Where(x => x.Variants.Any(v => v.IsActive && v.Color != null && colors.Contains(v.Color.Name.ToLower())));
        var sizes = Split(request.Sizes);
        if (sizes.Length > 0) query = query.Where(x => x.Variants.Any(v => v.IsActive && v.Size != null && (sizes.Contains(v.Size.Name.ToLower()) || sizes.Contains(v.Size.DisplayName.ToLower()))));
        query = collection switch
        {
            "featured" => query.Where(x => x.IsFeatured),
            "new-arrivals" => query.Where(x => x.IsNewArrival),
            "on-sale" => query.Where(x => x.OldPrice.HasValue && x.OldPrice > x.NewPrice),
            "related" => query.Where(x => x.CategoryId == request.CategorySlug),
            _ => query
        };
        query = request.Sort.ToLowerInvariant() switch
        {
            "popular" => query.OrderByDescending(x => x.RatingsCount).ThenByDescending(x => x.RatingsValue),
            "rating" => query.OrderByDescending(x => x.RatingsValue).ThenByDescending(x => x.RatingsCount),
            "price-asc" => query.OrderBy(x => x.NewPrice),
            "price-desc" => query.OrderByDescending(x => x.NewPrice),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
        var total = await query.CountAsync(ct);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken ct) => StorefrontProducts()
        .SingleOrDefaultAsync(x => x.Slug.ToLower() == slug.Trim().ToLower(), ct);

    private static string[] Split(string? values) => string.IsNullOrWhiteSpace(values) ? [] : values
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.ToLowerInvariant()).Distinct().ToArray();

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetAsync(ProductQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking().Include(x => x.Category).Include(x => x.Brand).Include(x => x.Images).AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Slug.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(request.CategoryId)) query = query.Where(x => x.CategoryId == request.CategoryId);
        if (!string.IsNullOrWhiteSpace(request.BrandId)) query = query.Where(x => x.BrandId == request.BrandId);
        query = request.Status?.ToLowerInvariant() switch
        {
            "draft" => query.Where(x => !x.IsArchived && x.PublishedAt == null),
            "active" => query.Where(x => !x.IsArchived && x.IsActive && x.PublishedAt != null),
            "inactive" => query.Where(x => !x.IsArchived && !x.IsActive && x.PublishedAt != null),
            "archived" => query.Where(x => x.IsArchived),
            _ => query
        };
        query = request.StockStatus?.ToLowerInvariant() switch
        {
            "in-stock" => query.Where(x => x.AvailabilityCount > request.LowStockThreshold),
            "low-stock" => query.Where(x => x.AvailabilityCount > 0 && x.AvailabilityCount <= request.LowStockThreshold),
            "out-of-stock" => query.Where(x => x.AvailabilityCount == 0),
            _ => query
        };
        if (request.MinPrice.HasValue) query = query.Where(x => x.NewPrice >= request.MinPrice.Value);
        if (request.MaxPrice.HasValue) query = query.Where(x => x.NewPrice <= request.MaxPrice.Value);
        query = request.Sort.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(x => x.CreatedAt),
            "name-asc" => query.OrderBy(x => x.Name),
            "name-desc" => query.OrderByDescending(x => x.Name),
            "price-asc" => query.OrderBy(x => x.NewPrice),
            "price-desc" => query.OrderByDescending(x => x.NewPrice),
            "stock-asc" => query.OrderBy(x => x.AvailabilityCount),
            "stock-desc" => query.OrderByDescending(x => x.AvailabilityCount),
            _ => query.OrderByDescending(x => x.CreatedAt)
        };
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<Product?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.Include(x => x.Category).Include(x => x.Brand).Include(x => x.Images).AsQueryable();
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
    public Task<bool> CategoryExistsAsync(string id, CancellationToken ct)
    {
        return dbContext.Categories.AnyAsync(x => x.Id == id && x.DeletedAt == null, ct);
    }

    public Task<bool> BrandExistsAsync(string id, CancellationToken ct)
    {
        return dbContext.Brands.AnyAsync(x => x.Id == id, ct);
    }

    public Task<bool> SlugExistsAsync(string slug, string? excludingId, CancellationToken ct)
    {
        return dbContext.Products.AnyAsync(x => x.Slug.ToLower() == slug.ToLower() && x.Id != excludingId, ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct)
    {
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct)
    {
        return dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct)
    {
        dbContext.Products.Remove(product);
        await dbContext.SaveChangesAsync(ct);
    }

    public Task<ProductImage?> GetImageAsync(string productId, string imageId, CancellationToken ct)
    {
        return dbContext.ProductImages.SingleOrDefaultAsync(x => x.ProductId == productId && x.Id == imageId, ct);
    }

    public async Task DeleteImageAsync(ProductImage image, CancellationToken ct)
    {
        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(ct);
    }
}
