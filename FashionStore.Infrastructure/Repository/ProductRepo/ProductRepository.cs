using FashionStore.Application.Abstractions.Products;
using FashionStore.Application.Dtos.Request;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.ProductRepo;

public sealed class ProductRepository(FashionStoreDbContext dbContext) : IProductRepository
{
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
