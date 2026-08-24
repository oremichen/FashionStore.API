using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.MainCarousels;

public interface IMainCarouselRepository
{
    Task<IReadOnlyList<MainCarousel>> GetAllAsync(CancellationToken cancellationToken);
    Task<MainCarousel?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task AddAsync(MainCarousel carousel, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(MainCarousel carousel, CancellationToken cancellationToken);
}
