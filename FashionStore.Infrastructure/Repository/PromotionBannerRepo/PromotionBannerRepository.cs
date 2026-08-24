using FashionStore.Domain.Repositories.PromotionBanners;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.PromotionBannerRepo;

public sealed class PromotionBannerRepository(FashionStoreDbContext dbContext) : IPromotionBannerRepository
{
    public async Task<IReadOnlyList<PromotionBanner>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.PromotionBanners.AsNoTracking().OrderBy(x => x.Slot).ToListAsync(cancellationToken);
    }

    public async Task<PromotionBanner?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.PromotionBanners : dbContext.PromotionBanners.AsNoTracking();
        return await query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> SlotExistsAsync(int slot, string? excludedId, CancellationToken cancellationToken)
    {
        return dbContext.PromotionBanners.AnyAsync(x => x.Slot == slot && (excludedId == null || x.Id != excludedId), cancellationToken);
    }

    public async Task AddAsync(PromotionBanner banner, CancellationToken cancellationToken) { dbContext.Add(banner); await dbContext.SaveChangesAsync(cancellationToken); }
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task DeleteAsync(PromotionBanner banner, CancellationToken cancellationToken) { dbContext.Remove(banner); await dbContext.SaveChangesAsync(cancellationToken); }
}
