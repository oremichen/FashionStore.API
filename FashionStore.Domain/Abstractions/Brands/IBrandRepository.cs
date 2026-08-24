using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Brands;

public interface IBrandRepository
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken);
    Task<Brand?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken);
    Task<bool> HasProductsAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(Brand brand, CancellationToken cancellationToken);
    Task DeleteAsync(Brand brand, CancellationToken cancellationToken);
}
