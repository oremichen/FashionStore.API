namespace FashionStore.Application.Abstractions.PromotionBanners;

public interface IPromotionBannerRepository
{
    Task<IReadOnlyList<PromotionBanner>> GetAllAsync(CancellationToken cancellationToken);
    Task<PromotionBanner?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<bool> SlotExistsAsync(int slot, string? excludedId, CancellationToken cancellationToken);
    Task AddAsync(PromotionBanner banner, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(PromotionBanner banner, CancellationToken cancellationToken);
}
