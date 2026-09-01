using FashionStore.Domain.Abstractions.CatalogOptions;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CatalogOptionRepo;

public sealed class CatalogOptionRepository(FashionStoreDbContext dbContext) : ICatalogOptionRepository
{
    public async Task<(IReadOnlyList<Size> Items, int TotalCount)> GetSizesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Sizes.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task<(IReadOnlyList<Color> Items, int TotalCount)> GetColorsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Colors.AsNoTracking();
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
