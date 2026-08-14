using FashionStore.Application.Abstractions.Brands;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.BrandRepo;

public sealed class BrandRepository(FashionStoreDbContext dbContext, ILogger<BrandRepository> logger) : IBrandRepository
{
    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying all brands.");
        return await dbContext.Brands
            .AsNoTracking()
            .OrderBy(brand => brand.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Brand?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying brand {BrandId}.", id);
        return await dbContext.Brands
            .AsNoTracking()
            .SingleOrDefaultAsync(brand => brand.Id == id, cancellationToken);
    }

    public async Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking brand uniqueness for slug {Slug}.", slug);
        return await dbContext.Brands.AnyAsync(
            brand => brand.Name.ToLower() == name.ToLower() || brand.Slug.ToLower() == slug.ToLower(),
            cancellationToken);
    }

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken)
    {
        logger.LogInformation("Persisting brand {BrandId}.", brand.Id);
        dbContext.Brands.Add(brand);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Persisted brand {BrandId}.", brand.Id);
    }
}
