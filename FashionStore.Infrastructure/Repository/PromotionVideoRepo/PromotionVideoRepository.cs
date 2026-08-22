using FashionStore.Application.Abstractions.PromotionVideos;
using FashionStore.Application.Dtos.Request;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.PromotionVideoRepo;

public sealed class PromotionVideoRepository(FashionStoreDbContext dbContext) : IPromotionVideoRepository
{
    public async Task<IReadOnlyList<PromotionVideo>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.PromotionVideos : dbContext.PromotionVideos.AsNoTracking();
        return await query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<PromotionVideo> Items, int TotalCount)> GetPagedAsync(
        PromotionVideoQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.PromotionVideos.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(term) || x.Slug.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<PromotionVideo?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.PromotionVideos : dbContext.PromotionVideos.AsNoTracking();
        return query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<PromotionVideo?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return dbContext.PromotionVideos.AsNoTracking().SingleOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public Task<PromotionVideo?> GetActiveAsync(CancellationToken cancellationToken)
    {
        return dbContext.PromotionVideos.AsNoTracking().SingleOrDefaultAsync(
            x => x.IsActive, cancellationToken);
    }

    public Task<bool> SlugExistsAsync(string slug, string? excludedId, CancellationToken cancellationToken)
    {
        return dbContext.PromotionVideos.AnyAsync(x => x.Slug == slug && (excludedId == null || x.Id != excludedId), cancellationToken);
    }

    public async Task AddAsync(PromotionVideo video, CancellationToken cancellationToken) { dbContext.Add(video); await dbContext.SaveChangesAsync(cancellationToken); }
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
    public async Task DeleteAsync(PromotionVideo video, CancellationToken cancellationToken) { dbContext.Remove(video); await dbContext.SaveChangesAsync(cancellationToken); }
}
