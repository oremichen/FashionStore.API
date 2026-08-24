using FashionStore.Domain.Repositories.MainCarousels;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.MainCarouselRepo;

public sealed class MainCarouselRepository(FashionStoreDbContext dbContext, ILogger<MainCarouselRepository> logger) : IMainCarouselRepository
{
    public async Task<IReadOnlyList<MainCarousel>> GetAllAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying main carousels.");
        return await dbContext.MainCarousels
            .AsNoTracking()
            .OrderBy(carousel => carousel.SortOrder)
            .ThenByDescending(carousel => carousel.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<MainCarousel?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying main carousel {CarouselId}; tracking: {TrackChanges}.", id, trackChanges);
        var query = trackChanges ? dbContext.MainCarousels : dbContext.MainCarousels.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task AddAsync(MainCarousel carousel, CancellationToken cancellationToken)
    {
        logger.LogInformation("Persisting main carousel {CarouselId}.", carousel.Id);
        dbContext.MainCarousels.Add(carousel);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Persisted main carousel {CarouselId}.", carousel.Id);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Saving main carousel changes.");
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(MainCarousel carousel, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting main carousel {CarouselId}.", carousel.Id);
        dbContext.MainCarousels.Remove(carousel);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted main carousel {CarouselId}.", carousel.Id);
    }
}
