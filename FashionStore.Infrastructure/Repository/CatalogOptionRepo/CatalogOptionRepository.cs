using FashionStore.Domain.Abstractions.CatalogOptions;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CatalogOptionRepo;

public sealed class CatalogOptionRepository(FashionStoreDbContext dbContext) : ICatalogOptionRepository
{
    public async Task<IReadOnlyDictionary<string, int>> GetSizeProductCountsAsync(CancellationToken cancellationToken)
    {
        var assignedSizes = dbContext.ProductSizes.AsNoTracking()
            .Where(mapping => !mapping.Product.IsArchived && mapping.Product.IsActive && mapping.Product.PublishedAt != null)
            .Select(mapping => new { mapping.SizeId, mapping.ProductId });
        var variantSizes = dbContext.ProductVariants.AsNoTracking()
            .Where(variant => variant.IsActive && variant.SizeId != null && !variant.Product.IsArchived && variant.Product.IsActive && variant.Product.PublishedAt != null)
            .Select(variant => new { SizeId = variant.SizeId!, variant.ProductId });
        var assignments = await assignedSizes.Concat(variantSizes).ToListAsync(cancellationToken);
        return assignments.GroupBy(assignment => assignment.SizeId)
            .ToDictionary(group => group.Key, group => group.Select(assignment => assignment.ProductId).Distinct().Count());
    }

    public async Task<IReadOnlyDictionary<string, int>> GetColorProductCountsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ProductColors.AsNoTracking()
            .Where(mapping => !mapping.Product.IsArchived && mapping.Product.IsActive && mapping.Product.PublishedAt != null)
            .GroupBy(mapping => mapping.ColorId)
            .ToDictionaryAsync(group => group.Key, group => group.Select(mapping => mapping.ProductId).Distinct().Count(), cancellationToken);
    }

    public async Task<(IReadOnlyList<Size> Items, int TotalCount)> GetSizesAsync(int page, int pageSize, bool availableOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Sizes.AsNoTracking();
        if (availableOnly)
            query = query.Where(size => size.IsActive &&
                (dbContext.ProductSizes.Any(mapping => mapping.SizeId == size.Id && !mapping.Product.IsArchived && mapping.Product.IsActive && mapping.Product.PublishedAt != null) ||
                 dbContext.ProductVariants.Any(variant => variant.SizeId == size.Id && variant.IsActive && !variant.Product.IsArchived && variant.Product.IsActive && variant.Product.PublishedAt != null)));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Color> Items, int TotalCount)> GetColorsAsync(int page, int pageSize, bool availableOnly, CancellationToken cancellationToken)
    {
        var query = dbContext.Colors.AsNoTracking();
        if (availableOnly)
            query = query.Where(color => color.IsActive &&
                dbContext.ProductColors.Any(mapping => mapping.ColorId == color.Id && !mapping.Product.IsArchived && mapping.Product.IsActive && mapping.Product.PublishedAt != null));
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<bool> SizeNameExistsAsync(string name, CancellationToken cancellationToken)
    {
        return dbContext.Sizes.AnyAsync(item => item.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
    }

    public Task<bool> SizeNameExistsAsync(string name, string excludeId, CancellationToken cancellationToken)
    {
        return dbContext.Sizes.AnyAsync(item => item.Id != excludeId && item.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
    }

    public Task<bool> ColorNameExistsAsync(string name, CancellationToken cancellationToken)
    {
        return dbContext.Colors.AnyAsync(item => item.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
    }

    public Task<Size?> GetSizeByIdAsync(string id, CancellationToken cancellationToken)
    {
        return dbContext.Sizes.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public Task<Color?> GetColorByIdAsync(string id, CancellationToken cancellationToken)
    {
        return dbContext.Colors.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<bool> SizeHasProductsAsync(string id, CancellationToken cancellationToken)
    {
        return await dbContext.ProductSizes.AnyAsync(item => item.SizeId == id, cancellationToken)
            || await dbContext.ProductVariants.AnyAsync(item => item.SizeId == id, cancellationToken);
    }

    public async Task<bool> ColorHasProductsAsync(string id, CancellationToken cancellationToken)
    {
        return await dbContext.ProductColors.AnyAsync(item => item.ColorId == id, cancellationToken)
            || await dbContext.ProductVariants.AnyAsync(item => item.ColorId == id, cancellationToken);
    }

    public async Task AddSizeAsync(Size size, CancellationToken cancellationToken)
    {
        dbContext.Sizes.Add(size);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddColorAsync(Color color, CancellationToken cancellationToken)
    {
        dbContext.Colors.Add(color);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteSizeAsync(Size size, CancellationToken cancellationToken)
    {
        dbContext.Sizes.Remove(size);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteColorAsync(Color color, CancellationToken cancellationToken)
    {
        dbContext.Colors.Remove(color);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
