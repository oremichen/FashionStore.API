namespace FashionStore.Application.Abstractions.PromotionVideos;

public interface IPromotionVideoRepository
{
    Task<IReadOnlyList<PromotionVideo>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken);
    Task<(IReadOnlyList<PromotionVideo> Items, int TotalCount)> GetPagedAsync(PromotionVideoQuery query, CancellationToken cancellationToken);
    Task<PromotionVideo?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<PromotionVideo?> GetBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<PromotionVideo?> GetActiveAsync(CancellationToken cancellationToken);
    Task<bool> SlugExistsAsync(string slug, string? excludedId, CancellationToken cancellationToken);
    Task AddAsync(PromotionVideo video, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(PromotionVideo video, CancellationToken cancellationToken);
}
