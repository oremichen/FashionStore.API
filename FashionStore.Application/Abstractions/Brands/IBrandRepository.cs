namespace FashionStore.Application.Abstractions.Brands;

public interface IBrandRepository
{
    Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken);
    Task<Brand?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken);
    Task AddAsync(Brand brand, CancellationToken cancellationToken);
}
