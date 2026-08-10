using FashionStore.Application.Abstractions.Brands;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.BrandRepo;

public sealed class BrandRepository(FashionStoreDbContext dbContext) : IBrandRepository
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Brand?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return await dbContext.Brands
            .AsNoTracking()
            .SingleOrDefaultAsync(brand => brand.Id == id, cancellationToken);
    }

    public async Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken)
    {
        return await dbContext.Brands.AnyAsync(
            brand => brand.Name.ToLower() == name.ToLower() || brand.Slug.ToLower() == slug.ToLower(),
            cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
