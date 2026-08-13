using FashionStore.Application.Abstractions.MainCarousels;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.MainCarouselRepo;

public sealed class MainCarouselRepository(FashionStoreDbContext dbContext) : IMainCarouselRepository
{
    public async Task<IReadOnlyList<MainCarousel>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.MainCarousels
            .AsNoTracking()
            .OrderBy(carousel => carousel.SortOrder)
            .ThenByDescending(carousel => carousel.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MainCarousel?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.MainCarousels : dbContext.MainCarousels.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(MainCarousel carousel, CancellationToken cancellationToken)
    {
        dbContext.MainCarousels.Add(carousel);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MainCarousel carousel, CancellationToken cancellationToken)
    {
        dbContext.MainCarousels.Remove(carousel);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
