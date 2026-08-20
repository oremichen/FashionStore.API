using FashionStore.Application.Abstractions.CatalogOptions;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.CatalogOptionRepo;

public sealed class CatalogOptionRepository(FashionStoreDbContext dbContext) : ICatalogOptionRepository
{
    public async Task<IReadOnlyList<Size>> GetSizesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Sizes
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Color>> GetColorsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Colors
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> SizeNameExistsAsync(string name, CancellationToken cancellationToken)
    {
        return dbContext.Sizes.AnyAsync(item => item.Name.ToLower() == name.Trim().ToLower(), cancellationToken);
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
