using TypeEntity = FashionStore.Domain.Entities.Type;

namespace FashionStore.Domain.Abstractions.Types;

public interface ITypeRepository
{
    Task<(IReadOnlyList<TypeEntity> Items, int TotalCount)> GetAllAsync(int page, int pageSize, bool activeOnly, CancellationToken cancellationToken);
    Task<TypeEntity?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken, string? excludedId = null);
    Task AddAsync(TypeEntity type, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task DeleteAsync(TypeEntity type, CancellationToken cancellationToken);
}
